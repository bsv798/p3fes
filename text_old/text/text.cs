using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace text
{
    public class ArchiveDescriptor
    {
        public readonly int type;
        public readonly string fileName;
        public readonly int offset;

        public ArchiveDescriptor(int type, string fileName, int offset)
        {
            this.type = type;
            this.fileName = fileName;
            this.offset = offset;
        }
    }

    public class TextArchive : Streams
    {
        public List<ArchiveDescriptor> descs;
        public List<TextItem> texts;

        public TextArchive()
        {
            descs = new List<ArchiveDescriptor>();
        }

        public virtual void LoadArchive()
        {
        }

        public virtual void SaveArchive()
        {
        }

        public void AddDescriptor(ArchiveDescriptor desc)
        {
            descs.Add(desc);
        }


        #region Overrides

        public static bool operator ==(TextArchive a, TextArchive b)
        {
            int cnt;
            int i;

            if (a.texts.Count != b.texts.Count)
                return false;

            cnt = a.texts.Count;
            for (i = 0; i < cnt; i++)
                if (a.texts[i] != b.texts[i])
                    return false;

            return true;
        }

        public static bool operator !=(TextArchive a, TextArchive b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        #endregion
    }

    public class TextItem : Streams
    {
        public List<StringItem> strings;

        public virtual void LoadText(bool isSelection, int count)
        {
        }

        public virtual void SaveText()
        {
        }


        #region Overrides

        public static bool operator ==(TextItem a, TextItem b)
        {
            int cnt;
            int i;

            if (a.strings.Count != b.strings.Count)
                return false;

            cnt = a.strings.Count;
            for (i = 0; i < cnt; i++)
                if (a.strings[i] != b.strings[i])
                    return false;

            return true;
        }

        public static bool operator !=(TextItem a, TextItem b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        #endregion
    }

    public class StringItem : Streams
    {
        public string orgText;
        public string rusText;
        public List<CommandItem> commands;

        public virtual void LoadString(int len)
        {
            rusText = orgText;
        }

        public virtual void SaveString(bool isSelection)
        {
        }


        #region Overrides

        public static bool operator ==(StringItem a, StringItem b)
        {
            int cnt;
            int i;

            if (a.commands.Count != b.commands.Count)
                return false;
            if (a.orgText.Length != b.orgText.Length)
                return false;

            cnt = a.commands.Count;
            for (i = 0; i < cnt; i++)
                if (a.commands[i] != b.commands[i])
                    return false;

            cnt = a.orgText.Length;
            for (i = 0; i < cnt; i++)
                if (a.orgText[i] != b.orgText[i])
                    return false;

            return true;
        }

        public static bool operator !=(StringItem a, StringItem b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        #endregion
    }

    public class CommandItem : Streams
    {
        public const char CommandChar = '‼';
        public string command;
        public int lenBytes;

        public CommandItem(int cmd)
        {
            lenBytes = (((cmd & 0xf) - 1) << 1) + 1;
            command = string.Format("{0:x2}", cmd);
            for (int i = 0; i < lenBytes; i++)
                command += string.Format("{0:x2}", reader.str.ReadByte());
        }

        public CommandItem(string cmd)
        {
            command = cmd.ToLower();
        }

        public void Save()
        {
            for (int i = 0; i < command.Length; i += 0x2)
                writer.str.WriteByte(Convert.ToByte(command.Substring(i, 0x2), 16));
        }

        public override string ToString()
        {
            return command;
        }


        #region Overrides

        public static bool operator ==(CommandItem a, CommandItem b)
        {
            int cnt;
            int i;

            if (a.command.Length != b.command.Length)
                return false;

            cnt = a.command.Length;
            for (i = 0; i < cnt; i++)
                if (a.command[i] != b.command[i])
                    return false;

            return true;
        }

        public static bool operator !=(CommandItem a, CommandItem b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        #endregion
    }
}
