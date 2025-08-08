using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BetterGenshinImpact.GameTask.AutoFight.Script;
using BetterGenshinImpact.GameTask.AutoFight.Model;
using BetterGenshinImpact.GameTask.AutoFight.Script;
using BetterGenshinImpact.Global;

namespace BetterGenshinImpact.GameTask.AutoFight.Script
{
    public static class CombatScriptParser
    {
        public static string CurrentAvatarName = "";

        public static CombatScript Parse(string path, bool validate = true)
        {
            if (!File.Exists(path))
            {
                Logger.LogError("战斗脚本文件不存在：{Path}", path);
                throw new Exception("战斗脚本文件不存在");
            }

            var lines = File.ReadAllLines(path)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            return ParseLines(lines, validate);
        }

        private static CombatScript ParseLines(List<string> lines, bool validate = true)
        {
            List<CombatCommand> combatCommands = new();
            HashSet<string> combatAvatarNames = new();

            foreach (var line in lines)
            {
                var trimLine = line.Trim();
                // 跳过注释行
                if (trimLine.StartsWith("//")) continue;
                // 跳过空行
                if (string.IsNullOrWhiteSpace(trimLine)) continue;

                var oneLineCombatCommands = ParseLine(trimLine, combatAvatarNames, validate);
                combatCommands.AddRange(oneLineCombatCommands);
            }

            var names = string.Join(",", combatAvatarNames);
            Logger.LogDebug("战斗脚本解析完成，共{Cnt}条指令，涉及角色：{Str}", combatCommands.Count, names);

            return new CombatScript(combatAvatarNames, combatCommands);
        }

        private static List<CombatCommand> ParseLine(string line, HashSet<string> combatAvatarNames, bool validate = true)
        {
            var oneLineCombatCommands = new List<CombatCommand>();
            // 以空格分隔角色和指令，截取第一个空格前的内容为角色名称，后面的为指令
            // 20241116更新 不输入角色名称时，直接以当前角色为准
            var firstSpaceIndex = line.IndexOf(' ');
            var character = CurrentAvatarName;
            var commands = line;
            if (firstSpaceIndex > 0)
            {
                character = line[..firstSpaceIndex];
                character = DefaultAutoFightConfig.AvatarAliasToStandardName(character);
                commands = line[(firstSpaceIndex + 1)..];
            }
            else
            {
                if (validate)
                {
                    Logger.LogError("战斗脚本格式错误，必须以空格分隔角色和指令");
                    throw new Exception("战斗脚本格式错误，必须以空格分隔角色和指令");
                }
            }

            oneLineCombatCommands.AddRange(ParseLineCommands(commands, character));
            combatAvatarNames.Add(character);
            return oneLineCombatCommands;
        }

        private static List<CombatCommand> ParseLineCommands(string lineWithoutAvatar, string avatarName)
        {
            var oneLineCombatCommands = new List<CombatCommand>();
            var commandArray = lineWithoutAvatar.Split(",", StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < commandArray.Length; i++)
            {
                var command = commandArray[i];
                if (string.IsNullOrEmpty(command))
                {
                    continue;
                }

                if (command.Contains('(') && !command.Contains(')'))
                {
                    var j = i + 1;
                    // 括号被逗号分隔，需要合并
                    while (j < commandArray.Length)
                    {
                        command += "," + commandArray[j];
                        if (command.Count("(".Contains) > 1)
                        {
                            Logger.LogError("战斗脚本格式错误，指令 {Cmd} 括号无法配对", command);
                            throw new Exception("战斗脚本格式错误，指令括号无法配对");
                        }

                        if (command.Contains(')'))
                        {
                            i = j;
                            break;
                        }

                        j++;
                    }

                    if (!(command.Contains('(') && command.Contains(')')))
                    {
                        Logger.LogError("战斗脚本格式错误，指令 {Cmd} 括号不完整", command);
                        throw new Exception("战斗脚本格式错误，指令括号不完整");
                    }
                }

                var combatCommand = new CombatCommand(avatarName, command);
                oneLineCombatCommands.Add(combatCommand);
            }

            return oneLineCombatCommands;
        }
    }
}
