using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace FESConfigFile
{
	public class FCG_Document
	{
		string Filepath = "";
		StringBuilder warning = new StringBuilder();
		int warningindex = 0;
		public bool TabToFourSpace = false;
		public FCG_Node RootNode;
		public bool Load(string Filepath)
		{
			warning.Clear();
			warningindex = 0;
			this.Filepath = Filepath;
			string filetxt = File.ReadAllText(Filepath);
			var ft = filetxt.AsSpan();
			int i = 0;
			//Read and Split the Text:ft
			while(i < ft.Length && ft[i] != '<')
			{
			    i++;
			}
			//<
			if(SplitNode(ref ft,ref i,out var rtn))
			{
				RootNode = rtn;
				RootNode.ParentNode = RootNode;
				return true;
			}
			else
			{
				AddWarning(warningindex,i,"RootNode is null");
				return false;
			}
		}
		bool SplitNode(ref ReadOnlySpan<char> ft,ref int i,out FCG_Node outnode)
		{
			warningindex++;
			var node = new FCG_Node("", "");
			var sb = new StringBuilder();
			//<
			if (i < ft.Length && Tot(ft[i]) == 2)
			{
				i++;
				//Remove space
				while(i < ft.Length && Tot(ft[i]) != 0)
				{
					i++;
				}
				//node.name
				while(i < ft.Length && Tot(ft[i]) == 0)
				{
					sb.Append(ft[i]);
					i++;
				}
                node.Name = sb.ToString();
                // > or \s
                while(i < ft.Length && Tot(ft[i]) != 3)
                {
                    //\s
                    while (i < ft.Length && Tot(ft[i]) == 1)
                    {
                        i++;
                    }
                    if (SplitAttribute(ref ft, ref i, out var atb))
                    {
						node.AppendAttribute(ref atb);
                    }
				}
				//>
				if (i < ft.Length && Tot(ft[i]) == 3)
				{
					i++;
					sb.Clear(); 
					//\s or letter
					while (i < ft.Length && Tot(ft[i]) == 1)
					{
						i++;
					}
					//node.value
					while (i < ft.Length && Tot(ft[i]) != 2)
					{
						if(Tot(ft[i]) != 1)
                        {
							sb.Append(ft[i]);
						}
						i++;
					}
					node.Value = sb.ToString();
					//<
					while(i < ft.Length && Tot(ft[i]) == 2)
                    {
						sb.Clear();
						if(ft[i + 1] == '/')
                        {
							//node end
							i += 2;
							while (i < ft.Length && Tot(ft[i]) != 3)
							{
								if (Tot(ft[i]) != 1)
								{
									sb.Append(ft[i]);
								}
								i++;
							}
							//finally check
							if(sb.ToString() == node.Name)
							{
								warningindex--;
								outnode = node;
								return true;
							}
							else
							{
								warningindex--;
								outnode = null;
								return false;
							}
						}
                        else
                        {
							//new node
							if(SplitNode(ref ft,ref i,out var cdnode))
                            {
								node.AppendNode(ref cdnode);
                            }
                        }
						while(i < ft.Length && Tot(ft[i]) != 2)
                        {
							i++;
                        }
					}
                    warningindex--;
                    outnode = null;
                    return false;
                }
                else
				{
					warningindex--;
					outnode = null;
					return false;
				}
			}
            else
            {
				warningindex--;
				outnode = null;
				return false;
			}
		}
		bool SplitAttribute(ref ReadOnlySpan<char> ft,ref int i,out FCG_Attribute atb)
		{
			string name, value;
			StringBuilder sb = new StringBuilder();
			//Atb.name
			while (i < ft.Length && Tot(ft[i]) == 0)
			{
				sb.Append(ft[i]);
				i++;
			}
			name = sb.ToString();
			//\s or =
			while (i < ft.Length && Tot(ft[i]) == 1)
			{
				i++;
			}
			//=
			if (Tot(ft[i]) == 4)
			{
				i++;
				sb.Clear();
				//\s
				while (i < ft.Length && Tot(ft[i]) == 1)
				{
					i++;
				}
				//Atb.value
				if (i < ft.Length && Tot(ft[i]) == 5)
				{
					i++;
					while (i < ft.Length && Tot(ft[i]) != 5)
					{
						sb.Append(ft[i]);
						i++;
					}
					i++;
				}
                else
                {
					while (i < ft.Length && Tot(ft[i]) == 0)
					{
						sb.Append(ft[i]);
						i++;
					}
				}
				value = sb.ToString();
				if (name != "" && value != "" && i < ft.Length)
				{
					atb = new FCG_Attribute(name, value);
					return true;
				}
				else
				{
					atb = null;
					return false;
				}
			}
            else
            {
				atb = null;
				return false;
            }
		}
		public void Save()
        {
            if(Filepath != "")
            {
                File.WriteAllText(Filepath,this.ToString());
            }
        }
        public void Save(string filepath)
        {
            this.Filepath = filepath;
            File.WriteAllText(Filepath,this.ToString());
        }
        public override string ToString()
        {
            return RootNode.ToString(0, TabToFourSpace);
        }
        void AddWarning(int wi,int i,string ws)
        {
            string s = "Warning(char:" + i.ToString() + ",node:" + wi.ToString() + ")" + ws + Environment.NewLine;
            warning.Append(s);
        }
        public string GetWarning()
        {
        	return warning.ToString();
        }
        //Sort by character
        //0:letter
        //1:\t\n\s\r
        //2:<
        //3:>
        //4:=
		//5:\"
        int Tot(char cr)
        {
            if(cr == ' ' || cr == '\n' || cr == '\t' || cr == '\r')
            {
                return 1;
            }
            else if(cr == '<')
            {
                return 2;
            }
            else if(cr == '>')
            {
                return 3;
            }
            else if(cr == '=')
            {
                return 4;
            }
			else if(cr == '\"')
            {
				return 5;
            }
            else
            {
                return 0;
            }
        }
		/// <summary>
		/// 通过路径来获取指定节点或属性的值，路径中节点通过/分割，属性通过:分割。
		/// 例如：
		/// <code>Root/path/node:atb</code>
		/// </summary>
		/// <param name="path">获取的路径</param>
		/// <param name="value">获取的值结果</param>
		/// <returns>返回获取是否成功</returns>
        public bool PathGetValue(string path,out string value)
        {
			var ps = path.AsSpan();
			var sb = new StringBuilder();
			List<string> pathnodes = new List<string>();
			string att = "";
			bool hasatt = false;
			int i = 0;

			while(i < ps.Length)
            {
				char s = ps[i];
				if(s == ':' && !hasatt)
                {
					string temp = sb.ToString();
					if(temp != "")
                    {
						pathnodes.Add(sb.ToString());
						sb.Clear();
					}
					hasatt = true;
                }
				else if(s == '/' && !hasatt)
                {
					string temp = sb.ToString();
					if (temp != "")
					{
						pathnodes.Add(sb.ToString());
						sb.Clear();
					}
				}
                else
                {
					if(s != ' ')
                    {
						sb.Append(s);
					}
                }
				i++;
            }
            if (hasatt && sb.Length != 0)
            {
				att = sb.ToString();
				sb.Clear();
			}
            else
            {
				pathnodes.Add(sb.ToString());
				sb.Clear();
			}
			//Search
			i = 0;
			FCG_Node now = RootNode;
			if(pathnodes.Count > 1 && pathnodes[i] == now.Name)
            {
				i++;
				while(i < pathnodes.Count)
                {
					int index = now.GetChildNodeIndex(pathnodes[i]);
					if(index != -1)
                    {
						now = now.ChildNodes[index];
                    }
                    else
                    {
						value = "null";
						return false;
                    }
					i++;
                }
				if (hasatt)
				{
					if (now.GetAttribute(att, out value))
					{
						return true;
					}
                    else
                    {
						return false;
                    }
				}
                else
                {
					value = now.Value;
					return true;
                }
			}
			else
			{
				value = "null";
				return false;
			}
		}
	}
	
	public class FCG_Node
	{
		string name;
		string value;
		public FCG_Node ParentNode;
		public List<FCG_Node> ChildNodes = new List<FCG_Node>();
		public List<FCG_Attribute> Attributes = new List<FCG_Attribute>();
		public FCG_Node(string name,string value)
		{
			this.name = name.Replace(" ","");
			this.value = value.Replace(" ","");
		}
		public string Name
		{
			get
			{
				return name;
			}
			set
			{
				this.name = value.Replace(" ","");
			}
		}
		public string Value
		{
			get
			{
				return value;
			}
			set
			{
				this.value = value.Replace(" ","");
			}
		}
		public bool AppendNode(ref FCG_Node node)
        {
            int i = 0;
            while(i < ChildNodes.Count)
            {
                if(ChildNodes[i].Name == node.Name)
                {
                    return false;
                }
                i++;
            }
			ChildNodes.Add(node);
			ChildNodes[ChildNodes.Count - 1].ParentNode = this;
			return true;
        }
		public void SetChildNode(string name,string value)
        {
			int i = 0;
			int index = -1;
			while(i < ChildNodes.Count)
            {
				if(ChildNodes[i].Name == name)
                {
					index = i;
					break;
                }
                i++;
            }
			if(index == -1)
            {
				FCG_Node node = new FCG_Node(name, value);
				ChildNodes.Add(node);
				ChildNodes[ChildNodes.Count - 1].ParentNode = this;
			}
            else
            {
				ChildNodes[index].Value = value;
            }
        }
		/// <summary>
		/// 获取子节点的索引值
		/// </summary>
		/// <param name="name"></param>
		/// <returns>返回获取的索引值，若没有该子节点则返回-1</returns>
		public int GetChildNodeIndex(string name)
        {
			int i = 0;
			while(i < ChildNodes.Count)
            {
				if(name  == ChildNodes[i].Name)
                {
					return i;
                }
				i++;
            }
			return -1;
        }
		public void RemoveNode(string name)
        {
			int i = 0;
			while(i < ChildNodes.Count)
            {
				if(ChildNodes[i].Name == name)
                {
					ChildNodes.RemoveAt(i);
					return;
                }
				i++;
            }
        }
		public bool AppendAttribute(ref FCG_Attribute atb)
        {
			int i = 0;
			while(i < Attributes.Count)
            {
				if(Attributes[i].Name == atb.Name)
                {
					return false;
                }
				i++;
            }
			Attributes.Add(atb);
			Attributes[Attributes.Count - 1].ParentNode = this;
			return true;
        }
		public void SetAttribute(string name,string value)
        {
			int i = 0;
			int index = -1;
			while(i < Attributes.Count)
            {
				if(Attributes[i].Name == name)
                {
					index = i;
					break;
                }
                i++;
            }
			if(index == -1)
            {
				FCG_Attribute sa = new FCG_Attribute(name, value);
				Attributes.Add(sa);
				Attributes[Attributes.Count - 1].ParentNode = this;
			}
            else
            {
				Attributes[index].Value = value;
            }
        }
        public bool GetAttribute(string name,out string value)
        {
            int i = 0;
            while(i < Attributes.Count)
            {
                if(name == Attributes[i].Name)
                {
                    value = Attributes[i].Value;
                    return true;
                }
				i++;
            }
            value = "null";
            return false;
        }
		public void RemoveAttribute(string name)
        {
			int i = 0;
			while(i < Attributes.Count)
            {
				if(Attributes[i].Name == name)
                {
					Attributes.RemoveAt(i);
					return;
                }
				i++;
            }
        }
		string Tab(int tab, bool TabToFourSpace)
        {
			StringBuilder ts = new StringBuilder();
			int i = 0;
			while(i < tab)
            {
				if(TabToFourSpace)
				{
					ts.Append("    ");
				}
				else
				{
					ts.Append("\t");
				}
				i++;
            }
			return ts.ToString();
        }
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("<");
			sb.Append(name);
			int i = 0;
			while(i < Attributes.Count)
			{
				sb.Append(" ");
				sb.Append(Attributes[i].ToString());
				i++;
			}
			sb.Append(">");
			if(value != "")
            {
				sb.Append(value);
			}
			sb.Append("</");
			sb.Append(name);
			sb.Append(">");
			return sb.ToString();
		}
		public string ToString(int tab, bool TabToFourSpace)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(Environment.NewLine + Tab(tab, TabToFourSpace) +  "<");
			sb.Append(name);
			int i = 0;
			while (i < Attributes.Count)
			{
				sb.Append(" ");
				sb.Append(Attributes[i].ToString());
				i++;
			}
			sb.Append(">");
			if(value != "")
			{
				sb.Append(Environment.NewLine);
				sb.Append(Tab(tab + 1, TabToFourSpace));
				sb.Append(value);
			}
			i = 0;
			while (i < ChildNodes.Count)
            {
				sb.Append(ChildNodes[i].ToString(tab + 1, TabToFourSpace));
				i++;
			}
			sb.Append(Environment.NewLine);
			sb.Append(Tab(tab, TabToFourSpace) + "</");
			sb.Append(name);
			sb.Append(">");
			return sb.ToString();
		}
	}
	public class FCG_Attribute
	{
		string name;
		string value;
		public FCG_Node ParentNode;
		public FCG_Attribute(string name,string value)
		{
			this.name = name.Replace(" ","");
			this.value = value;
		}
		public string Name
		{
			get
			{
				return name;
			}
			set
			{
				this.name = value.Replace(" ","");
			}
		}
		public string Value
		{
			get
			{
				return value;
			}
			set
			{
				this.value = value;
			}
		}
		public override string ToString()
		{
			return name + "=" + value;
		}
	}
}