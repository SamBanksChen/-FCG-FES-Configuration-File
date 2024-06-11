using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace FESConfigFile
{
    public class FCG_LightCfg
    {
        public enum CollectionType
        {
            cList, cDictionary
        }
        public enum LineType
        {
            vprompt,vvalue,vclass
        }
        public struct LineRef
        {
            public LineRef(LineType lineType, string lineinfo)
            {
                this.lineType = lineType;
                this.lineinfo = lineinfo;
            }
            public LineType lineType;
            public string lineinfo;
        }
        public class LightClass
        {
            public LightClass(object value, CollectionType type)
            {
                this.value = value;
                this.type = type;
            }
            public LightClass(List<string> value)
            {
                this.value = value;
                type = CollectionType.cList;
            }
            public LightClass(Dictionary<string, string> value)
            {
                this.value = value;
                type = CollectionType.cDictionary;
            }
            public object value;
            public CollectionType type;
            public static explicit operator List<string>(LightClass lc)
            {
                if (lc.value is List<string>)
                {
                    return lc.value as List<string>;
                }
                else
                {
                    return null;
                }
            }
            public static explicit operator Dictionary<string, string>(LightClass lc)
            {
                if(lc.value is Dictionary<string, string>)
                {
                    return lc.value as Dictionary<string, string>;
                }
                else
                {
                    return null;
                }
            }
        }
        public const byte SyntaxVersion = 1;
        public const byte StableVersion = 0;
        public const byte PatchVersion = 0;
        public static string version => SyntaxVersion.ToString() + "." + StableVersion.ToString() + "." + PatchVersion.ToString();

        string Filepath = "";
        List<LineRef> LineRefs = new List<LineRef>();
        Dictionary<string, string> Values = new Dictionary<string, string>();
        Dictionary<string, LightClass> Classes = new Dictionary<string, LightClass>();
        public bool Open(string Filepath)
        {
            var Lines = new List<string>();
            this.Filepath = Filepath;
            var sr = new StreamReader(Filepath);
            var vinfo = sr.ReadLine().AsSpan();
            int index = vinfo.IndexOf('D');
            if (byte.Parse(vinfo[index + 1].ToString()) != SyntaxVersion && byte.Parse(vinfo[index + 2].ToString()) > StableVersion)
            {
                return false;
            }
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if(line != "")Lines.Add(line);
            }
            sr.Close();
            ToVars(Lines);
            return true;
        }
        public bool Parse(byte SyntaxVersion,byte StableVersion,string text)
        {
            var Lines = new List<string>();
            if (SyntaxVersion != FCG_LightCfg.SyntaxVersion && StableVersion > FCG_LightCfg.StableVersion)
            {
                return false;
            }
            var span = text.AsSpan();
            var sb = new StringBuilder();
            foreach (char c in span)
            {
                if(c != '\r')
                {
                    if (c == '\n' && sb.Length > 0)
                    {
                        Lines.Add(sb.ToString());
                        sb.Clear();
                    }
                    else if(c != '\n') 
                    {
                        sb.Append(c);
                    }
                }
            }
            if (sb.Length > 0)
            {
                Lines.Add(sb.ToString());
                sb.Clear();
            }
            ToVars(Lines);
            return true;
        }
        void ToVars(List<string> lines)
        {
            var sb = new StringBuilder();
            string name = "", val = "";
            bool needNew = false;
            bool isClass = false;
            CollectionType lastType = CollectionType.cList;
            List<string> list = new List<string>();
            Dictionary<string, string> dict = new Dictionary<string, string>();
            for (int ii = 0; ii < lines.Count; ii++)
            {
                var span = lines[ii].AsSpan();
                sb.Clear();
                int i = 0;
                //去除 \s\t
                while (i < span.Length && (span[i] == ' ' || span[i] == '\t'))
                {
                    i++;
                }
                if (span[i] == '#')
                {
                    LineRefs.Add(new LineRef(LineType.vprompt, lines[ii]));
                    continue;
                }//记录注释
                while (i < span.Length)
                {
                    int key = ToKey(span[i]);
                    if (key == 1)
                    {
                        if (span[i] == '@')
                        {
                            name = sb.ToString();
                            sb.Clear();
                            needNew = true;
                            isClass = true;
                        }//类
                        else if (span[i] == ':')
                        {
                            name = sb.ToString();
                            sb.Clear();
                            needNew = true;
                            isClass = false;
                        }//值
                    }
                    else if(key == 2)
                    {
                        if (span[i] == ',')
                        {
                            val = sb.ToString();
                            sb.Clear();
                            i++;
                            if (needNew)
                            {
                                needNew = false;
                                isClass = true;
                                lastType = CollectionType.cList;
                                list = new List<string>();
                                Classes.Add(name, new LightClass(list));
                                LineRefs.Add(new LineRef(LineType.vclass, name));
                                list.Add(val);
                            }
                            else
                            {
                                if(isClass && lastType == CollectionType.cList)
                                {
                                    list.Add(val);
                                }
                            }
                            while(i < span.Length)
                            {
                                if (span[i] == ',')
                                {
                                    if (isClass && lastType == CollectionType.cList)
                                    {
                                        list.Add(sb.ToString());
                                    }
                                    sb.Clear();
                                }
                                else
                                {
                                    sb.Append(span[i]);
                                }
                                i++;
                            }
                            if (isClass && lastType == CollectionType.cList)
                            {
                                list.Add(sb.ToString());
                            }
                            sb.Clear();
                        }//数组
                        else if (span[i] == '=')
                        {
                            val = sb.ToString();
                            sb.Clear();
                            i++;
                            while(i < span.Length)
                            {
                                sb.Append(span[i]);
                                i++;
                            }
                            if (needNew)
                            {
                                needNew = false;
                                isClass = true;
                                lastType = CollectionType.cDictionary;
                                dict = new Dictionary<string, string>();
                                Classes.Add(name, new LightClass(dict));
                                LineRefs.Add(new LineRef(LineType.vclass, name));
                                dict.Add(val,sb.ToString());
                            }
                            else
                            {
                                if (isClass && lastType == CollectionType.cDictionary)
                                {
                                    dict.Add(val, sb.ToString());
                                }
                            }
                            sb.Clear();
                        }//键值对
                    }
                    else
                    {
                        sb.Append(span[i]);
                    }
                    i++;
                }
                if(sb.Length > 0)
                {
                    if (sb.Length > 0 && needNew && !isClass)
                    {
                        Values.Add(name, sb.ToString());
                        LineRefs.Add(new LineRef(LineType.vvalue, name));
                        sb.Clear();
                        needNew = false;
                    }
                    else if(sb.Length > 0 && isClass && lastType == CollectionType.cList)
                    {
                        if (needNew)
                        {
                            needNew = false;
                            isClass = true;
                            lastType = CollectionType.cList;
                            list = new List<string>();
                            Classes.Add(name, new LightClass(list));
                            LineRefs.Add(new LineRef(LineType.vclass, name));
                        }
                        list.Add(sb.ToString());
                        sb.Clear();
                    }
                }
            }
            return;
        }
        //return  0 word  1 @:  2 ,=  3 "
        int ToKey(char c)
        {
            switch (c)
            {
                case '@':
                    return 1;
                case ':':
                    return 1;
                case ',':
                    return 2;
                case '=':
                    return 2;
                default:
                    return 0;
            }
        }
        public void Clear()
        {
            Filepath = "";
            Values.Clear();
            LineRefs.Clear();
            foreach(var kv in Classes)
            {
                if(kv.Value.type == CollectionType.cList)
                {
                    (kv.Value.value as List<string>).Clear();
                }
                else
                {
                    (kv.Value.value as Dictionary<string, string>).Clear();
                }
            }
        }
        public List<string> ToLines()
        {
            var lines = new List<string>();
            foreach (var rf in LineRefs)
            {
                if (rf.lineType == LineType.vprompt)
                {
                    lines.Add(rf.lineinfo);
                }
                else if (rf.lineType == LineType.vvalue)
                {
                    lines.Add(rf.lineinfo + ":" + Values[rf.lineinfo]);
                }
                else if (rf.lineType == LineType.vclass)
                {
                    lines.Add(rf.lineinfo + "@");
                    LightClass lc = Classes[rf.lineinfo];
                    if (lc.type == CollectionType.cList)
                    {
                        var list = lc.value as List<string>;
                        var sb = new StringBuilder();
                        for (int i = 0; i < list.Count; i++)
                        {
                            string str = list[i];
                            sb.Append(str);
                            if (i != list.Count - 1) sb.Append(",");
                        }
                        lines.Add(sb.ToString());
                    }
                    else if (lc.type == CollectionType.cDictionary)
                    {
                        var dict = lc.value as Dictionary<string, string>;
                        foreach (var kv in dict)
                        {
                            lines.Add(kv.Key + "=" + kv.Value);
                        }
                    }
                }
            }
            return lines;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach(var rf in LineRefs)
            {
                if(rf.lineType == LineType.vprompt)
                {
                    sb.AppendLine(rf.lineinfo);
                }
                else if(rf.lineType == LineType.vvalue)
                {
                    sb.Append(rf.lineinfo).Append(":").AppendLine(Values[rf.lineinfo]);
                }
                else if(rf.lineType == LineType.vclass)
                {
                    sb.Append(rf.lineinfo).AppendLine("@");
                    LightClass lc = Classes[rf.lineinfo];
                    if(lc.type == CollectionType.cList)
                    {
                        var list = lc.value as List<string>;
                        for (int i = 0; i < list.Count; i++)
                        {
                            string str = list[i];
                            sb.Append(str);
                            if (i != list.Count - 1) sb.Append(",");
                        }
                        sb.Append(Environment.NewLine);
                    }
                    else if(lc.type == CollectionType.cDictionary)
                    {
                        var dict = lc.value as Dictionary<string,string>;
                        foreach (var kv in dict)
                        {
                            sb.Append(kv.Key).Append("=").AppendLine(kv.Value);
                        }
                    }
                }
            }
            return sb.ToString();
        }
        public string ToTextFile()
        {
            return "D" + SyntaxVersion.ToString() + StableVersion.ToString() + Environment.NewLine;
        }
        public bool Save()
        {
            if(Filepath != "" && File.Exists(Filepath))
            {
                File.WriteAllText(Filepath, ToTextFile());
                return true;
            }
            return false;
        }
        public bool Save(string path)
        {
            if (path != "" && File.Exists(path))
            {
                Filepath = path;
                File.WriteAllText(Filepath, ToTextFile());
                return true;
            }
            return false;
        }
        public bool TryGetValue(string key, out string value)
        {
            if (Values.ContainsKey(key))
            {
                value = Values[key];
                return true;
            }
            else
            {
                value = "";
                return false;
            }
        }
        public bool TryGetList(string key, out List<string> list)
        {
            if(Classes.ContainsKey(key) && Classes[key].type == CollectionType.cList)
            {
                list = Classes[key].value as List<string>;
                return true;
            }
            else
            {
                list = null;
                return false;
            }
        }
        public bool TryGetDictionary(string key, out  Dictionary<string, string> dict)
        {
            if(Classes.ContainsKey(key) && Classes[key].type == CollectionType.cDictionary)
            {
                dict = Classes[key].value as Dictionary<string, string>;
                return true;
            }
            else
            {
                dict = null;
                return false;
            }
        }
    }
}
