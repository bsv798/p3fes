#define JINN

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace text
{
    public class MsgTextArchive : TextArchive
    {
        public new List<MsgTextItem> texts;
        public string[] names;

        public MsgTextArchive()
            : base()
        {
            texts = new List<MsgTextItem>();
        }

        private void LoadNames()
        {
            int txtCount;
            int namePos;
            int nameCount;
            byte[] buf;
            int bufCnt;

            reader.str.Position = offset + 0x18;
            txtCount = reader.binRead.ReadInt32();
            reader.binRead.ReadInt32();

            reader.str.Position += txtCount << 0x3;
            namePos = reader.binRead.ReadInt32() + 0x20;
            nameCount = reader.binRead.ReadInt32();

            names = new string[nameCount];
            buf = new byte[0x100];
            for (int i = 0; i < nameCount; i++)
            {
                reader.str.Position = offset + namePos;
                reader.str.Position = offset + reader.binRead.ReadInt32() + 0x20;
                for (bufCnt = 0; bufCnt < 0x100; bufCnt++)
                    if ((buf[bufCnt] = (byte)reader.str.ReadByte()) == 0)
                        break;
                names[i] = reader.ReadStringUsingTable(buf, 0, bufCnt); //reader.str.ReadNullTerminatedString(misc.japEnc); 
                namePos += 0x4;
            }
        }

        public override void LoadArchive()
        {
            int ptrOff;
            int ptrCnt;
            byte[] ptrs;
            int curPtr;
            int txtCnt;
            int[] txtPtrs;
            bool[] sels;
            int strCnt;
            int i, j;

            reader.str.Position = offset + 0x10;
            ptrOff = reader.binRead.ReadInt32();
            ptrCnt = reader.binRead.ReadInt32() + 1;
            txtCnt = reader.binRead.ReadInt32();

            sels = new bool[txtCnt];
            txtPtrs = new int[txtCnt];
            reader.binRead.ReadInt32();
            for (i = 0; i < txtCnt; i++)
            {
                sels[i] = (reader.binRead.ReadInt32() == 1) ? true : false;
                txtPtrs[i] = reader.binRead.ReadInt32() + 0x20;
            }

            ptrs = new byte[ptrCnt + 1];
            reader.str.Position = offset + ptrOff;
            reader.str.Read(ptrs, 0, ptrCnt - 1);

            curPtr = txtCnt + 1;
            for (i = 0; i < txtCnt; i++)
            {
                strCnt = 1;
                for (j = curPtr; j < ptrCnt; j++, curPtr++, strCnt++)
                {

                    if ((ptrs[j] & 1) != 0)
                    {
                        if ((ptrs[j] & 0x2) != 0)
                        {
                            if ((ptrs[j] & 0x4) != 0)
                            {
                                strCnt += (ptrs[j] >> 0x3) + 0x1;
                                //ptrOff = 1;
                            }
                            else  //ptr & 4 == 0
                            {
                                curPtr += 0x2;
                                //ptrOff = (ptrs[j++] | (ptrs[j++] << 0x8) | (ptrs[j] << 0x10)) >> 0x3;
                                j += 0x2;
                            }
                        }
                        else //ptr & 2 == 0
                        {
                            curPtr++;
                            //ptrOff = (ptrs[j++] | (ptrs[j] << 0x8)) >> 0x2;
                            j++;
                        }
                    }
                    else //ptr & 1 == 0
                    {
                        //ptrOff = ptrs[j] >> 0x1;
                    }

                    //ptrOff <<= 0x2;
                    ptrOff = GetPtrOff(ptrs, j + 1) << 0x2;
                    if (ptrOff != 0x4)
                    {
                        reader.str.Position = offset + txtPtrs[i];
                        texts.Add(new MsgTextItem(sels[i], strCnt));
                        curPtr++;
                        break;
                    }
                }
            }

            LoadNames();
        }

#if JINN
        #region Jinn

        enum jinnMainState { na, files, texts }
        enum jinnTextState { na, names, msgs }

        public void JinnLoadText()
        {
            jinnMainState mainState;
            jinnTextState textState;
            MsgTextItem mti;
            MsgStringItem msi;
            string s = reader.strRead.ReadLine();
            int pp;

            names = new string[0];
            mainState = jinnMainState.na;
            textState = jinnTextState.na;
            mti = null;
            msi = null;
            while (s != null)
            {
                //s = System.Text.Encoding.Unicode.GetString(System.Text.Encoding.Convert(System.Text.Encoding.UTF8, System.Text.Encoding.Unicode, System.Text.Encoding.UTF8.GetBytes(s)));
                if (s.Length > 0)
                {
                    if (s[0] == '#')
                        mainState = JinnGetMainState(s, mainState);
                    else if (!JinnIsComment(s)) // & s[1] != '/')
                        switch (mainState)
                        {
                            case jinnMainState.files:
                                break;
                            case jinnMainState.texts:
                                switch (textState)
                                {
                                    case jinnTextState.names:
                                        Array.Resize<string>(ref names, names.Length + 1);
                                        names[names.Length - 1] = (s.EndsWith("\\e")) ? s.Substring(0, s.Length - 0x2) : s;
                                        break;
                                    case jinnTextState.msgs:
                                        if (mti.isSelection)
                                        {
                                            msi.orgText += JinnReplaceCommands((s.EndsWith("\\e")) ? s.Substring(0, s.Length - 0x2) : s, msi);
                                            if (s.EndsWith("\\e"))
                                            {
                                                if (msi.orgText.Length > 0)
                                                    msi.orgText = msi.orgText.Substring(0, msi.orgText.Length - 0x1);
                                                msi.rusText = msi.orgText;
                                                mti.strings.Add(msi);
                                                mti.ptrCnt = (short)mti.strings.Count;
                                                msi = new MsgStringItem();
                                            }
                                        }
                                        else if ((pp = s.IndexOf("\\p")) > -1)
                                        {
                                            msi.rusText = JinnReplaceCommands(s.Substring(0, pp), msi);
                                            if (msi.rusText.EndsWith("\n"))
                                                msi.rusText = msi.rusText.Remove(msi.rusText.Length - 1, 1);
                                            if (pp == s.Length - 0x2)
                                                msi.commands.Add(new CommandItem("F104"));
                                            else if (s[pp + 0x2] == '1')
                                                msi.commands.Add(new CommandItem("F110"));
                                            else if (s[pp + 0x2] == '2')
                                                msi.commands.Add(new CommandItem("F131"));
                                            else
                                                throw new ArgumentException(string.Format("error in string: {0}", s));
                                            msi.orgText += msi.rusText + CommandItem.CommandChar;
                                            msi.rusText = msi.orgText;
                                            mti.strings.Add(msi);
                                            mti.ptrCnt = (short)mti.strings.Count;
                                            msi = new MsgStringItem();
                                        }
                                        else if (s == "\\e")
                                        {
                                            msi.rusText = msi.orgText;
                                            mti.strings.Add(msi);
                                            mti.ptrCnt = (short)mti.strings.Count;
                                            msi = new MsgStringItem();
                                        }
                                        else
                                            msi.orgText += JinnReplaceCommands(s, msi);
                                        break;
                                    default:
                                        textState = JinnGetTextState(s, textState);
                                        if (textState == jinnTextState.msgs)
                                        {
                                            mti = new MsgTextItem();
                                            mti.textId = JinnGetValue(s, 0).Remove(0, 2);
                                            mti.isSelection = JinnGetValue(s, 2)[0] == '1';
                                            if (!mti.isSelection)
                                                mti.nameIdx = Convert.ToInt16(JinnGetValue(s, 1));
                                            texts.Add(mti);
                                            msi = new MsgStringItem();
                                            break;
                                        }
                                        break;
                                } //switch (textState)
                                break;
                            default:
                                break;
                        } //switch (mainState)
                }
                else
                    textState = jinnTextState.na;
                s = reader.strRead.ReadLine();
            }
        }

        private jinnMainState JinnGetMainState(string str, jinnMainState currState)
        {
            if (str.Length > 0)
                if (str[0] == '#')
                    switch (str)
                    {
                        case "#FILES_LIST#":
                            return jinnMainState.files;
                        case "#TEXT_LINES#":
                            return jinnMainState.texts;
                        default:
                            throw new ArgumentException(string.Format("Unknoun main id: %s", str));
                    }
            return currState;
        }

        private jinnTextState JinnGetTextState(string str, jinnTextState currState)
        {
            if (str.Length > 1)
                if (str[0] == '@' & str[1] == '@')
                {
                    if (str == "@@NAMES_LIST")
                        return jinnTextState.names;
                    else if (str.Replace(":", "").Length == str.Length - 0x2)
                        return jinnTextState.msgs;
                    else
                        throw new ArgumentException(string.Format("Unknoun text id: %s", str));
                }
            return currState;
        }

        private string JinnGetValue(string paramStr, int paramIdx)
        {
            return paramStr.Split(':')[paramIdx];
        }

        private string JinnReplaceCommands(string str, MsgStringItem msi)
        {
            string cmd;
            int cnt;
            int i1, i2;
            int b;

            while ((i1 = str.IndexOf('{') + 1) > 0)
                if ((i2 = str.IndexOf('}', i1)) > -1)
                {
                    cmd = str.Substring(i1, i2 - i1);
                    switch (cmd[0])
                    {
                        case 'v': //+
                            msi.commands.Add(new CommandItem(string.Format("F203{0:x2}01", Convert.ToByte(cmd.Substring(1, cmd.Length - 1)))));
                            break;
                        case 'c': //+
                            msi.commands.Add(new CommandItem(string.Format("F202{0:x2}01", Convert.ToByte(cmd.Substring(1, cmd.Length - 1)))));
                            break;
                        case 'N':
                            if (cmd == "Name") //+
                                msi.commands.Add(new CommandItem("F10B"));
                            else
                                goto default;
                            break;
                        case 'V':
                            b = Convert.ToByte(cmd.Substring(5, cmd.Length - 5));
                            if (b == 1) //+
                                msi.commands.Add(new CommandItem("F31A01010201"));
                            else if (b == 2) //+
                                msi.commands.Add(new CommandItem("F31A03010401"));
                            else
                                goto default;
                            break;
                        case 'I':
                            b = Convert.ToByte(cmd.Substring(4, cmd.Length - 4));
                            if (b == 1) //+
                                msi.commands.Add(new CommandItem("F31A04010301"));
                            else if (b == 2) //+
                                msi.commands.Add(new CommandItem("F31A04010501"));
                            else
                                goto default;
                            break;
                        case 'S':
                            if (cmd == "Sayonara") //+
                                msi.commands.Add(new CommandItem("F22C0101"));
                            else if ((b = cmd.IndexOf(',') + 1) > 0) //+
                                msi.commands.Add(new CommandItem(string.Format("F31A{0:x2}01{1:x2}01", Convert.ToByte(cmd.Substring(3, b - 4)) + 0x0d, Convert.ToByte(cmd.Substring(b, cmd.Length - b)))));
                            else
                                goto default;
                            break;
                        case 'C': //+
                            msi.commands.Add(new CommandItem(string.Format("F22E{0:x2}01", Convert.ToByte(cmd.Substring(4, cmd.Length - 4)))));
                            break;
                        case 'P':
                            msi.commands.Add(new CommandItem(string.Format("F21D{0}", cmd.Substring(5, cmd.Length - 5))));
                            break;
                        case 'T': //+
                            msi.commands.Add(new CommandItem(string.Format("F219{0:x2}01", Convert.ToByte(cmd.Substring(5, cmd.Length - 5)))));
                            break;
                        case 'H': //+
                            msi.commands.Add(new CommandItem(string.Format("F214{0:x2}01", Convert.ToByte(cmd.Substring(4, cmd.Length - 4)))));
                            break;
                        default:
                            if (cmd == "LastName") //+
                                msi.commands.Add(new CommandItem("F10A"));
                            else if (cmd == "FullName") //+
                                msi.commands.Add(new CommandItem("F10C"));
                            else if (cmd == "Date") //+
                                msi.commands.Add(new CommandItem("F138"));
                            else
                                throw new ArgumentException(string.Format("Unknown command: {0}", cmd));
                            break;
                    }
                    str = str.Substring(0, i1 - 1) + CommandItem.CommandChar + str.Substring(++i2, str.Length - i2);
                }

            cnt = 0;
            while ((i1 = str.IndexOf('[') + 1) > 0)
                if ((i2 = str.IndexOf(']', i1)) > -1)
                {
                    cmd = str.Substring(i1, i2 - i1);
                    if (cmd.Length % 2 == 0)
                        msi.commands.Add(new CommandItem(cmd));
                    else
                        throw new ArgumentException(string.Format("Unknown command: {0}", cmd));
                    str = str.Substring(0, i1 - 1) + CommandItem.CommandChar + str.Substring(++i2, str.Length - i2);
                    cnt++;
                }
            if (cnt != str.Length)
                str += '\n';

            return str;
        }

        private bool JinnIsComment(string str)
        {
            if (str.Length < 2)
                return false;
            else if (str[0] == '/' & str[1] == '/')
                return true;
            else
                return false;
        }

        #endregion Jinn
#endif

        private int GetPtrOff(byte[] ptrs, int j)
        {
            if ((ptrs[j] & 1) != 0)
            {
                if ((ptrs[j] & 0x2) != 0)
                {
                    if ((ptrs[j] & 0x4) != 0)
                    {
                        return 1;
                    }
                    else  //ptr & 4 == 0
                    {
                        return (ptrs[j++] | (ptrs[j++] << 0x8) | (ptrs[j] << 0x10)) >> 0x3;
                    }
                }
                else //ptr & 2 == 0
                {
                    return (ptrs[j++] | (ptrs[j] << 0x8)) >> 0x2;
                }
            }
            else //ptr & 1 == 0
            {
                return ptrs[j] >> 0x1;
            }
        }

        private void SaveNames()
        {
            long tblPtrPos = writer.str.Position;
            int[] ptrs = new int[names.Length];
            long endPos;
            int i;

            writer.str.Position += names.Length << 0x2;
            for (i = 0; i < names.Length; i++)
            {
                ptrs[i] = (int)writer.str.Position - offset - 0x20;
                writer.binWrite.Write(writer.WriteStringUsingTable(names[i])); //japEnc.GetBytes(names[i]));
                writer.str.WriteByte(0);
            }
            writer.str.AlignWrite(0x4);
            endPos = writer.str.Position;

            writer.str.Position = tblPtrPos;
            for (i = 0; i < ptrs.Length; i++)
                writer.binWrite.Write(ptrs[i]);
            writer.str.Position = endPos;
        }

        public override void SaveArchive()
        {
            long txtTblPos;
            long txtPos;
            long txtLen;
            byte[] ptrs;
            int curPtr;
            int cnt;

            writer.binWrite.Write(0x7);
            writer.binWrite.Write(0);
            writer.binWrite.Write(0x3147534D); //MSG1
            writer.binWrite.Write(0);

            writer.binWrite.Write(0);
            writer.binWrite.Write(0);
            writer.binWrite.Write(texts.Count);
            writer.binWrite.Write(0x00020000);

            ptrs = new byte[8192];
            curPtr = 0;
            if (texts.Count == 1)
            {
                ptrs[curPtr++] = 0x7;
            }
            else
            {
                ptrs[0] = 0x2;
                for (curPtr = 1; curPtr < texts.Count; curPtr++)
                    ptrs[curPtr] = 0x4;
                ptrs[curPtr++] = 0x2;
            }
            ptrs[curPtr++] = 0x16;

            txtTblPos = writer.str.Position;
            writer.str.Position += (texts.Count + 2) << 0x3;
            txtPos = writer.str.Position;
            cnt = 0;
            txtLen = 0;
            foreach (var text in texts)
            {
                //if (text.strings[0].orgText.StartsWith("‼‼‼Please do not fear. You and I will"))
                //    writer.str.Position = txtTblPos;
                writer.str.Position = txtTblPos;
                if (text.isSelection)
                    writer.binWrite.Write(1);
                else
                    writer.binWrite.Write(0);
                writer.binWrite.Write((int)txtPos - offset - 0x20);
                txtTblPos += 0x8;

                writer.str.Position = txtPos;
                text.SaveText();

                txtLen = writer.str.Position - txtPos - ((text.strings.Count - 1) << 0x2);
                if (text.isSelection)
                    txtLen -= 0x4;
                if (++cnt < texts.Count)
                    if (texts[cnt].isSelection)
                        txtLen += 0x4;
                SetPtr(ptrs, ref curPtr, txtLen, text.strings.Count);

                txtPos = writer.str.Position;
            }

            writer.str.Position = txtTblPos;
            writer.binWrite.Write((int)txtPos - offset - 0x20);
            writer.binWrite.Write(names.Length);

            writer.str.Position = txtPos;
            SaveNames();
            if (names.Length > 0)
            {
                ptrs[curPtr - 1] -= 0x1c / 2;
                txtLen = writer.str.Position - txtPos - 0x1c;
                SetPtr(ptrs, ref curPtr, txtLen, names.Length);
            }
            curPtr--;
            if (txtLen > 0x1ff)
                curPtr--;

            txtPos = writer.str.Position;
            writer.str.Write(ptrs, 0, curPtr);

            txtLen = writer.str.Position - offset;
            writer.str.Position = offset + 0x4;
            writer.binWrite.Write((int)txtLen);

            writer.str.Position += 0x8;
            writer.binWrite.Write((int)txtPos);
            writer.binWrite.Write(curPtr);

            writer.str.Position = txtPos + curPtr;
        }

        private void SetPtr(byte[] ptrs, ref int curPtr, long txtLen, int strCnt)
        {
            if (strCnt > 1)
                if (strCnt == 0x2)
                    ptrs[curPtr++] = 0x2;
                else if (strCnt < 0x23)
                    ptrs[curPtr++] = (byte)(((strCnt - 0x3) << 0x3) | 0x7);
                else if (strCnt < 0x45)
                {
                    ptrs[curPtr++] = (byte)((0x1f << 0x3) | 0x7);
                    ptrs[curPtr++] = (byte)(((strCnt - 0x21 - 0x3) << 0x3) | 0x7);
                }
                else
                    throw new NotImplementedException("strings count exceed 0x45");

            if (txtLen < 0x200)
            {
                txtLen >>= 1;
                ptrs[curPtr++] = (byte)txtLen;
            }
            else if (txtLen < 0x10000)
            {
                txtLen |= 1;
                ptrs[curPtr++] = (byte)(txtLen & 0xff);
                ptrs[curPtr++] = (byte)((txtLen >> 0x8) & 0xff);
            }
            else
                throw new NotImplementedException("txtLen exceeds 65535");
        }


        #region Overrides

        public static bool operator ==(MsgTextArchive a, MsgTextArchive b)
        {
            int cnt;
            int i;

            if (a.names.Length != b.names.Length)
                return false;
            if (a.texts.Count != b.texts.Count)
                return false;

            cnt = a.names.Length;
            for (i = 0; i < cnt; i++)
                if (a.names[i].Length != b.names[i].Length)
                    return false;

            for (i = 0; i < cnt; i++)
                if (a.names[i] != b.names[i])
                    return false;

            cnt = a.texts.Count;
            for (i = 0; i < cnt; i++)
                if (a.texts[i] != b.texts[i])
                    return false;

            return true;
        }

        public static bool operator !=(MsgTextArchive a, MsgTextArchive b)
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

    public class MsgTextItem : TextItem
    {
        public bool isSelection;
        public string textId;
        public short ptrCnt;
        public short nameIdx;
        public int textLen;
        public new List<MsgStringItem> strings;

        public MsgTextItem()
        {
            strings = new List<MsgStringItem>();
        }

        public MsgTextItem(bool isSelection, int count)
            : this()
        {
            LoadText(isSelection, count);
        }

        public override void LoadText(bool isSelection, int count)
        {
            this.isSelection = isSelection;
            textId = reader.str.ReadNullTerminatedString(0x18, misc.japEnc);
            if (isSelection)
            {
                reader.binRead.ReadInt16();
                nameIdx = 0;
                ptrCnt = reader.binRead.ReadInt16();
                reader.binRead.ReadInt32();
            }
            else
            {
                ptrCnt = reader.binRead.ReadInt16();
                nameIdx = reader.binRead.ReadInt16();
            }
            ReadStrings(ptrCnt);
        }

        private void ReadStrings(int cnt)
        {
            int[] pos = new int[cnt + 1];
            int i;

            for (i = 0; i < cnt; i++)
                pos[i] = reader.binRead.ReadInt32() + 0x20;
            pos[i] = int.MaxValue;

            textLen = reader.binRead.ReadInt32();

            for (i = 0; i < cnt; i++)
            {
                reader.str.Position = offset + pos[i];
                strings.Add(new MsgStringItem(pos[i + 1] - pos[i]));
            }
        }

        public override void SaveText()
        {
            writer.str.WriteNullTerminatedString(textId, 0x18, japEnc);
            if (isSelection)
            {
                writer.binWrite.Write((short)0);
                writer.binWrite.Write((short)strings.Count);
                writer.binWrite.Write(0);
                WriteStrings();
            }
            else
            {
                writer.binWrite.Write((short)strings.Count);
                writer.binWrite.Write(nameIdx);
                WriteStrings();
            }
        }

        private void WriteStrings()
        {
            long tblPtrPos = writer.str.Position;
            int[] ptrs = new int[strings.Count];
            long endPos;
            int i;

            writer.str.Position += (strings.Count + 1) << 0x2;
            textLen = (int)writer.str.Position;
            for (i = 0; i < strings.Count; i++)
            {
                ptrs[i] = (int)writer.str.Position - offset - 0x20;
                strings[i].SaveString(isSelection);
            }
            writer.str.WriteByte(0);
            textLen = (int)writer.str.Position - textLen;
            writer.str.AlignWrite(0x4);
            endPos = writer.str.Position;

            writer.str.Position = tblPtrPos;
            for (i = 0; i < ptrs.Length; i++)
                writer.binWrite.Write(ptrs[i]);
            writer.binWrite.Write(textLen);
            writer.str.Position = endPos;
        }

        #region Overrides

        public static bool operator ==(MsgTextItem a, MsgTextItem b)
        {
            int cnt;
            int i;

            if (a.isSelection != b.isSelection)
                return false;
            if (a.textId != b.textId)
                return false;
            if (a.ptrCnt != b.ptrCnt)
                return false;
            if (a.nameIdx != b.nameIdx)
                return false;
            //if (a.textLen != b.textLen)
            //    return false;
            if (a.strings.Count != b.strings.Count)
                return false;

            cnt = a.strings.Count;
            for (i = 0; i < cnt; i++)
                if (a.strings[i] != b.strings[i])
                    return false;

            return true;
        }

        public static bool operator !=(MsgTextItem a, MsgTextItem b)
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

        public override string ToString()
        {
            return textId;
        }
        #endregion
    }

    public class MsgStringItem : StringItem
    {
        public MsgStringItem()
        {
            orgText = "";
            commands = new List<CommandItem>();
        }

        public MsgStringItem(int len)
            : this()
        {
            LoadString(len);
        }

        public override void LoadString(int len)
        {
            CommandItem cmd;
            byte[] bb = new byte[1024];
            byte b;
            int pos = -1;
            int cnt = 0;
            int i = 0;

            b = reader.binRead.ReadByte();
            bb[i] = b;
            while (i++ < len && b != 0)
            {
                if (b > 0xef)
                {
                    if (cnt > 0)
                    {
                        orgText += reader.ReadStringUsingTable(bb, pos, cnt); // japEnc.GetString(bb, pos, cnt);
                        pos = -1;
                        cnt = 0;
                    }
                    cmd = new CommandItem(b);
                    commands.Add(cmd);
                    orgText += CommandItem.CommandChar;
                    i += cmd.lenBytes;
                }
                else
                {
                    if (pos < 0)
                        pos = i - 1;
                    if (b < 0x80)
                    {
                        cnt++;
                    }
                    else
                    {
                        bb[i++] = reader.binRead.ReadByte();
                        cnt += 0x2;
                    }
                }
                b = reader.binRead.ReadByte();
                bb[i] = b;
            }
            if (cnt > 0)
                orgText += reader.ReadStringUsingTable(bb, pos, cnt); //japEnc.GetString(bb, pos, cnt);

            base.LoadString(0);
        }

        public override void SaveString(bool isSelection)
        {
            byte[] orgBuf;
            //byte[] rusBuf = new byte[1024];
            //int orgCnt;
            //int rusCnt;
            int txtPos = -1;
            int txtLen = 0;
            int cmdCnt = 0;
            int i;

            for (i = 0; i < rusText.Length; i++)
            {
                if (rusText[i] == CommandItem.CommandChar)
                {
                    if (txtLen > 0)
                    {
                        //rusCnt = 0;
                        orgBuf = writer.WriteStringUsingTable(rusText.Substring(txtPos, txtLen)); //japEnc.GetBytes(rusText.Substring(txtPos, txtLen));
                        writer.str.Write(orgBuf, 0, orgBuf.Length);
                        if (isSelection)
                            writer.str.WriteByte(0);
                        //for (orgCnt = 0; orgCnt < orgBuf.Length; orgCnt++)
                        //{
                        //    rusBuf[rusCnt++] = orgBuf[orgCnt];
                        //}
                        //writer.str.Write(rusBuf, 0, rusCnt);

                        txtPos = -1;
                        txtLen = 0;
                    }
                    commands[cmdCnt++].Save();
                }
                else if (txtPos < 0)
                {
                    txtPos = i;
                    txtLen = 1;
                }
                else
                {
                    txtLen++;
                }
            }
            if (txtLen > 0)
            {
                orgBuf = writer.WriteStringUsingTable(rusText.Substring(txtPos, txtLen)); //japEnc.GetBytes(rusText.Substring(txtPos, txtLen));
                writer.str.Write(orgBuf, 0, orgBuf.Length);
            }
            if (isSelection)
                writer.str.WriteByte(0);
        }

        public override string ToString()
        {
            return orgText;
        }

        public string JinnToText()
        {
            string s = orgText;
            int c = 0;
            for (int i = 0; i < s.Length; i++)
                if (s[i] == CommandItem.CommandChar)
                    s = s.Substring(0, i) + '[' + commands[c++].command.ToUpper() + ']' + '\n' + s.Substring(++i, s.Length - i);
            return s;
        }

        #region Overrides

        public static bool operator ==(MsgStringItem a, MsgStringItem b)
        {
            return (StringItem)a == (StringItem)b;
        }

        public static bool operator !=(MsgStringItem a, MsgStringItem b)
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
