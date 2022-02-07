using Cobra.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Cobra.Woodpecker8
{
    public class EFUSEConfigDEMBehaviorManage : DEMBehaviorManageBase
    {
        public override UInt32 Command(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch ((ElementDefine.COMMAND)msg.sub_task)
            {
                case ElementDefine.COMMAND.EFUSE_CONFIG_READ:
                    {
                        ParamContainer demparameterlist = msg.task_parameterlist;
                        if (demparameterlist == null) return ret;
                        if (msg.task_parameterlist.parameterlist.Count < ElementDefine.EF_TOTAL_PARAMS)
                            return ElementDefine.IDS_ERR_DEM_ONE_PARAM_DISABLE;
                        ret = GetRegisteInfor(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        msg.percent = 40;
                        ret = SetWorkMode(ElementDefine.WORK_MODE.PROGRAM);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        msg.gm.message = "Please provide 7.2V power supply to Tref pin, and limit its current to 80mA.";
                        msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                        if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;
                        msg.percent = 60;

                        byte offset = 0;
                        if (isEFBank2Empty() == true)
                        {
                            offset = 0;
                            //System.Windows.Forms.MessageBox.Show("Reading bank1.");
                            msg.gm.message = "Reading bank1.";
                            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                        }
                        else
                        {
                            offset = 4;
                            //System.Windows.Forms.MessageBox.Show("Reading bank2.");
                            msg.gm.message = "Reading bank2.";
                            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                        }
                        List<byte> OpReglist = Utility.GenerateRegisterList(ref msg);
                        byte bdata = 0;
                        foreach (byte badd in OpReglist)
                        {
                            if (badd == 0x16 || badd == 0x26)
                            {
                                ret = ReadByte(badd, ref bdata);
                                parent.m_OpRegImg[badd].err = ret;
                                parent.m_OpRegImg[badd].val = (UInt16)bdata;
                            }
                            else
                            {
                                ret = ReadByte((byte)(badd + offset), ref bdata);
                                parent.m_OpRegImg[(byte)(badd)].err = ret;
                                parent.m_OpRegImg[(byte)(badd)].val = (UInt16)bdata;
                            }
                        }
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        msg.percent = 80;
                        msg.gm.message = "Please remove 7.2V power supply from Tref pin.";
                        msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                        if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;
                        ret = SetWorkMode(ElementDefine.WORK_MODE.NORMAL);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        ret = ConvertHexToPhysical(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        break;
                    }
                case ElementDefine.COMMAND.EFUSE_CONFIG_WRITE:
                    {
                        bool isConfigEmpty = true;
                        byte offset = 0;
                        byte operatingbank = 1;
                        byte bdata = 0;
                        ParamContainer demparameterlist = msg.task_parameterlist;
                        if (demparameterlist == null) return ret;
                        if (msg.task_parameterlist.parameterlist.Count < ElementDefine.EF_TOTAL_PARAMS)
                            return ElementDefine.IDS_ERR_DEM_ONE_PARAM_DISABLE;


                        List<byte> OpReglist = Utility.GenerateRegisterList(ref msg);
                        ret = SafetyCheck(OpReglist);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        msg.percent = 30;
                        ret = GetRegisteInfor(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        ret = SetWorkMode(ElementDefine.WORK_MODE.PROGRAM);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;

                        msg.gm.message = "Please provide 7.2V power supply to Tref pin, and limit its current to 80mA.";
                        msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                        if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                        if (isEFConfigEmpty() == false)
                        {
                            isConfigEmpty = false;
                            OpReglist.Remove(0x16);
                            //System.Windows.Forms.MessageBox.Show("Config register 0x16 is frozen. Skip to program it.");
                            msg.gm.message = "Register 0x16 is frozen. Skip to program it.";
                            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                        }
                        else
                        {
                            isConfigEmpty = true;
                            parent.m_OpRegImg[0x16].val |= 0x80;
                        }

                        if (isEFBank1Empty() == true)
                        {
                            offset = 0;
                            //System.Windows.Forms.MessageBox.Show("Writing bank1.");
                            msg.gm.message = "Writing bank1.";
                            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                            operatingbank = 1;
                        }
                        else if (isEFBank2Empty() == true)
                        {
                            offset = 4;
                            //System.Windows.Forms.MessageBox.Show("Bank1 is frozen, writing bank2.");
                            msg.gm.message = "Bank1 is frozen, writing bank2.";
                            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                            operatingbank = 2;
                        }
                        else
                        {
                            //System.Windows.Forms.MessageBox.Show("Bank1 and bank2 are frozen, stop writing.");
                            //return 0;
                            msg.gm.message = "Please remove 7.2V power supply from Tref pin.";
                            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                            if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                            ret = SetWorkMode(ElementDefine.WORK_MODE.NORMAL);
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                                return ret;
                            return ElementDefine.IDS_ERR_DEM_FROZEN;
                        }

                        #region Read
                        foreach (byte badd in OpReglist)
                        {
                            if (badd == 0x16)
                            {
                                ret = ReadByte(badd, ref bdata);
                                parent.m_OpRegImg[badd].err = ret;
                                parent.m_OpRegImg[badd].val = (UInt16)bdata;
                            }
                            else
                            {
                                ret = ReadByte((byte)(badd + offset), ref bdata);
                                parent.m_OpRegImg[(byte)(badd)].err = ret;
                                parent.m_OpRegImg[(byte)(badd)].val = (UInt16)bdata;
                            }
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                                return ret;
                        }
                        #endregion

                        #region Phy2Hex
                        ret = ConvertPhysicalToHex(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        #endregion

                        if (operatingbank == 1)
                            parent.m_OpRegImg[0x1b].val |= 0x80;
                        else if (operatingbank == 2)
                            parent.m_OpRegImg[0x1b].val |= 0x80;    //m_OpRegImg 0x1c~0x1f are not used

                        if (isConfigEmpty)
                            parent.m_OpRegImg[0x16].val |= 0x80;

                        #region Write
                        foreach (byte badd in OpReglist)
                        {
                            if (badd == 0x16)
                            {
                                ret = WriteByte(badd, (byte)parent.m_OpRegImg[badd].val);
                            }
                            else
                                ret = WriteByte((byte)(badd + offset), (byte)parent.m_OpRegImg[badd].val);
                            parent.m_OpRegImg[badd].err = ret;
                        }
                        #endregion

                        #region Read
                        foreach (byte badd in OpReglist)
                        {
                            if (badd == 0x16)
                            {
                                ret = ReadByte(badd, ref bdata);
                                parent.m_OpRegImg[badd].err = ret;
                                parent.m_OpRegImg[badd].val = (UInt16)bdata;
                            }
                            else
                            {
                                ret = ReadByte((byte)(badd + offset), ref bdata);
                                parent.m_OpRegImg[(byte)(badd)].err = ret;
                                parent.m_OpRegImg[(byte)(badd)].val = (UInt16)bdata;
                            }
                            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                                return ret;
                        }
                        #endregion

                        msg.gm.message = "Please remove 7.2V power supply from Tref pin.";
                        msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_SELECT;
                        if (!msg.controlmsg.bcancel) return LibErrorCode.IDS_ERR_DEM_USER_QUIT;

                        ret = SetWorkMode(ElementDefine.WORK_MODE.NORMAL);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;

                        ret = ConvertHexToPhysical(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        return ret;
                    }
                case ElementDefine.COMMAND.EFUSE_CONFIG_SAVE_EFUSE_HEX:
                    {
                        InitEfuseData();
                        ret = ConvertPhysicalToHex(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        PrepareHexData();
                        ret = GetEfuseHexData(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        FileStream hexfile = new FileStream(msg.sub_task_json, FileMode.Create);
                        StreamWriter hexsw = new StreamWriter(hexfile);
                        hexsw.Write(msg.sm.efusehexdata);
                        hexsw.Close();
                        hexfile.Close();

                        ret = GetEfuseBinData(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;

                        string binfilename = Path.Combine(Path.GetDirectoryName(msg.sub_task_json),
                            Path.GetFileNameWithoutExtension(msg.sub_task_json) + ".bin");

                        Encoding ec = Encoding.UTF8;
                        using (BinaryWriter bw = new BinaryWriter(File.Open(binfilename, FileMode.Create), ec))
                        {
                            foreach (var b in msg.sm.efusebindata)
                                bw.Write(b);

                            bw.Close();
                        }
                        break;
                    }
            }
            return ret;
        }
        #region Save Hex
        private void InitEfuseData()
        {
            parent.m_OpRegImg[ElementDefine.EF_CFG].err = 0;
            parent.m_OpRegImg[ElementDefine.EF_CFG].val = 0;
            for (ushort i = ElementDefine.EF_USR_OFFSET; i <= ElementDefine.EF_USR_TOP; i++)
            {
                parent.m_OpRegImg[i].err = 0;
                parent.m_OpRegImg[i].val = 0;
            }
        }

        private void PrepareHexData()
        {
            //if (cfgFRZ == false)
            parent.m_OpRegImg[ElementDefine.EF_CFG].val |= 0x80;    //Set Frozen bit in image

            //if (bank1FRZ == false)
            parent.m_OpRegImg[ElementDefine.EF_USR_BANK1_TOP].val |= 0x80;    //Set Frozen bit in image
        }
        private UInt32 GetEfuseHexData(ref TASKMessage msg)
        {
            string tmp = "";
            if (parent.m_OpRegImg[ElementDefine.EF_CFG].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return parent.m_OpRegImg[ElementDefine.EF_CFG].err;
            tmp += "0x" + ElementDefine.EF_CFG.ToString("X2") + ", " + "0x" + parent.m_OpRegImg[ElementDefine.EF_CFG].val.ToString("X2") + "\r\n";
            for (ushort i = ElementDefine.EF_USR_BANK1_OFFSET; i <= ElementDefine.EF_USR_BANK1_TOP; i++)
            {
                if (parent.m_OpRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return parent.m_OpRegImg[i].err;
                tmp += "0x" + i.ToString("X2") + ", " + "0x" + parent.m_OpRegImg[i].val.ToString("X2") + "\r\n";
            }
            msg.sm.efusehexdata = tmp;
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        private UInt32 GetEfuseBinData(ref TASKMessage msg)
        {
            List<byte> tmp = new List<byte>();
            if (parent.m_OpRegImg[ElementDefine.EF_CFG].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return parent.m_OpRegImg[ElementDefine.EF_CFG].err;
            tmp.Add((byte)ElementDefine.EF_CFG);
            tmp.Add((byte)(parent.m_OpRegImg[ElementDefine.EF_CFG].val));
            for (ushort i = ElementDefine.EF_USR_BANK1_OFFSET; i <= ElementDefine.EF_USR_BANK1_TOP; i++)
            {
                if (parent.m_OpRegImg[i].err != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return parent.m_OpRegImg[i].err;
                tmp.Add((byte)i);
                tmp.Add((byte)(parent.m_OpRegImg[i].val));
            }
            msg.sm.efusebindata = tmp;
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        private bool isEFConfigEmpty()
        {
            byte tmp = 0;
            ReadByte(0x16, ref tmp);
            if ((tmp & 0x80) == 0x80)
                return false;
            else
                return true;
        }

        private bool isEFBank1Empty()
        {
            byte tmp = 0;
            ReadByte(0x1b, ref tmp);
            if ((tmp & 0x80) == 0x80)
                return false;
            else
                return true;
        }

        private bool isEFBank2Empty()
        {
            byte tmp = 0;
            ReadByte(0x1f, ref tmp);
            if ((tmp & 0x80) == 0x80)
                return false;
            else
                return true;
        }
        #endregion
    }
}
