using System;
using System.Collections;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text.Json.Nodes;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;

namespace ParseCCBase
{
    class ParseCCBase
    {
        static void Main()
        {
            // Read input from a .txt file. Ask user for input.
            Console.WriteLine("Welcome! You will need to enter in the location of your file.");
            Console.Write("Enter in the ABSOLUTE file path of the base txt file: ");
            string? file_path = Console.ReadLine();
            file_path = file_path.Trim('"');
            
            ArrayList file_lines = new ArrayList();

            // Try to retrieve the text from the file
            try
            {
                StreamReader sr = new StreamReader(file_path);
                string cur_line;
                do
                {
                    cur_line = sr.ReadLine();
                    file_lines.Add(cur_line);
                } while (cur_line != null);
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            // Parse and convert to JSON
            JsonObject parsed_json = parse_to_json(file_lines);

            // Retrieve a list of characters, locations, and songs
            HashSet<string> characters = get_items(parsed_json, 1);
            HashSet<string> locations = get_items(parsed_json, 2);
            HashSet<string> songs = get_items(parsed_json, 3);

            // Save the parsed JSON information to a folder
            string? output_file_dir = Path.GetDirectoryName(file_path);
            string? output_dir = Path.Combine(output_file_dir, "Output");

            string output_path = Path.Combine(output_dir, "Casual_Roleplay");

            if (!Directory.Exists(output_dir))
            {
                Directory.CreateDirectory(output_dir);
            }

            JsonSerializerOptions json_options = new JsonSerializerOptions();
            json_options.WriteIndented = true;
            File.WriteAllText(output_path + ".json", parsed_json.ToJsonString(json_options));
            Console.WriteLine("Parsed JSON has been saved to " + output_path + ".json.");

            File.WriteAllLines(output_path + "_characters.txt.", characters.Cast<string>());
            Console.WriteLine("Characters have been saved to " + output_path + "_characters.txt.");
            File.WriteAllLines(output_path + "_locations.txt.", locations.Cast<string>());
            Console.WriteLine("Locations have been saved to " + output_path + "_locations.txt.");
            File.WriteAllLines(output_path + "_songs.txt.", songs.Cast<string>());
            Console.WriteLine("Songs have been saved to " + output_path + "_songs.txt.");
        }

        static JsonObject parse_to_json(ArrayList file_lines)
        {
            JsonObject info = new JsonObject();

            string last_season = null;
            string last_episode = null;
            string last_attribute = null;
            string last_content = null;
            string last_specific = null;

            foreach (string line in file_lines)
            {
                if (line == null) {
                    continue;
                }

                bool starts_with_star = line.StartsWith("*");
                bool starts_with_space = line.StartsWith(" ");
                int amount_leading_space = line.Length - line.TrimStart().Length;

                if (!(starts_with_star || starts_with_space)) // Season
                {
                    string trimmed_line = line.TrimStart('\uFEFF').Trim();
                    info.Add(trimmed_line, new JsonObject());
                    last_season = trimmed_line;
                }
                else if (starts_with_star)  // Episode
                {
                    string trimmed_line = line.Replace('*', ' ').Trim();
                    JsonObject? cur_season = info[last_season] as JsonObject;
                    cur_season.Add(trimmed_line, new JsonObject());
                    last_episode = trimmed_line;
                }
                else if (starts_with_space && amount_leading_space == 3) // Attribute
                {
                    string trimmed_line = line.Replace('*', ' ').Trim();
                    JsonObject? cur_season = info[last_season] as JsonObject;
                    JsonObject? cur_episode = cur_season[last_episode] as JsonObject;

                    if (trimmed_line.Equals("Songs"))
                    {
                        cur_episode.Add(trimmed_line, new JsonObject());
                    }
                    else
                    {
                        cur_episode.Add(trimmed_line, new JsonArray());
                    }

                    last_attribute = trimmed_line;
                }
                else if (starts_with_space && amount_leading_space == 6) // Content
                {
                    string trimmed_line = line.Replace('*', ' ').Trim();
                    JsonObject? cur_season = info[last_season] as JsonObject;
                    JsonObject? cur_episode = cur_season[last_episode] as JsonObject;
                    
                    if (last_attribute.Equals("Songs"))
                    {
                        JsonObject? cur_attribute = cur_episode[last_attribute] as JsonObject;
                        cur_attribute.Add(trimmed_line, new JsonObject());
                    }
                    else
                    {
                        JsonArray? cur_attr_array = cur_episode[last_attribute] as JsonArray;
                        cur_attr_array.Add(JsonValue.Create(trimmed_line));
                    }

                    last_content = trimmed_line;
                }
                else if (starts_with_space && amount_leading_space == 9) // Specific
                {
                    string trimmed_line = line.Replace('*', ' ').Trim();
                    JsonObject? cur_season = info[last_season] as JsonObject;
                    JsonObject? cur_episode = cur_season[last_episode] as JsonObject;
                    JsonObject? cur_attribute = cur_episode[last_attribute] as JsonObject;
                    JsonObject? cur_content = cur_attribute[last_content] as JsonObject;

                    if (last_content.Equals("Scene Specific"))
                    {
                        cur_content.Add(trimmed_line, new JsonObject());
                    }
                    else
                    {
                        cur_attribute.Remove(last_content);
                        cur_attribute.Add(last_content, JsonValue.Create(trimmed_line));
                    }

                    last_specific = trimmed_line;
                }
                else if (starts_with_space && amount_leading_space == 12)
                {
                    string trimmed_line = line.Replace('*', ' ').Trim();
                    JsonObject? cur_season = info[last_season] as JsonObject;
                    JsonObject? cur_episode = cur_season[last_episode] as JsonObject;
                    JsonObject? cur_attribute = cur_episode[last_attribute] as JsonObject;
                    JsonObject? cur_content = cur_attribute[last_content] as JsonObject;
                    cur_content.Remove(last_specific);
                    cur_content.Add(last_specific, JsonValue.Create(trimmed_line));
                }
            }

            // Remove extra stuff
            info.Remove("Chapter Template");
            info.Remove("Extra Songs");

            return info;
        }

        static HashSet<string> get_items(JsonObject info, int item)
        {
            // 1 for characters, 2 for locations, 3 for songs

            HashSet<string> set_list = new HashSet<string>();

            foreach (KeyValuePair<string, JsonNode?> season in info)
            {   
                JsonObject? season_obj = season.Value as JsonObject;
                foreach (KeyValuePair<string, JsonNode?> episode in season_obj)
                {
                    JsonObject? episode_obj = episode.Value as JsonObject;

                    switch (item)
                    {
                        case 1:
                            JsonArray? ep_items = episode_obj["Characters"] as JsonArray;
                            foreach (JsonValue? ep_item in ep_items)
                            {
                                set_list.Add((string?) ep_item);
                            }
                            continue;
                        case 2:
                            ep_items = episode_obj["Locations"] as JsonArray;
                            foreach (JsonValue? ep_item in ep_items)
                            {
                                set_list.Add((string?) ep_item);
                            }
                            continue;
                        case 3:
                            JsonObject? ep_songs = episode_obj["Songs"] as JsonObject;

                            JsonValue? intro_song = ep_songs["Intro Song"] as JsonValue;
                            JsonValue? outro_song = ep_songs["Outro Song"] as JsonValue;

                            set_list.Add((string?) intro_song);
                            set_list.Add((string?) outro_song);

                            JsonObject? ep_scenes = ep_songs["Scene Specific"] as JsonObject;
                            foreach (KeyValuePair<string, JsonNode?> cur_scene in ep_scenes)
                            {
                                set_list.Add(cur_scene.Value.ToString());
                            }
                            continue;
                    }
                }
            }

            return set_list;
        }
    }
}