using Cobra.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cobra.Woodpecker8
{
    internal class MassProductionDEMBehaviorManage : DEMBehaviorManageBase
    {

        private bool bank1FRZ = false, bank2FRZ = false, cfgFRZ = false;
        byte[] EFUSEUSRbuf = new byte[ElementDefine.EF_USR_BANK1_TOP - ElementDefine.EF_USR_BANK1_OFFSET + 1 + 1]; //bank1的长度，加上0x16    0x16放到EFUSEUSRbuf[4]里面去
        private byte WritingBank1Or2 = 0;   //bank1
        #region 基础服务功能设计
        public override UInt32 Command(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;


            switch ((ElementDefine.COMMAND)msg.sub_task)
            {

                case ElementDefine.COMMAND.MP_FROZEN_BIT_CHECK_PC:
                    ret = PowerOn();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;

                    ret = FrozenBitCheck();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;

                    ret = PowerOff();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;

                case ElementDefine.COMMAND.MP_FROZEN_BIT_CHECK:
                    ret = FrozenBitCheck();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;

                case ElementDefine.COMMAND.MP_DIRTY_CHIP_CHECK_PC:
                    ret = PowerOn();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;

                    ret = DirtyChipCheck();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;

                    ret = PowerOff();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;

                case ElementDefine.COMMAND.MP_DIRTY_CHIP_CHECK:
                    ret = DirtyChipCheck();
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;

                case ElementDefine.COMMAND.MP_DOWNLOAD_PC:
                    {
                        ret = Download(ref msg, msg.sm.efusebindata, true);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
#if debug
                        Thread.Sleep(1000);
#endif
                        break;
                    }

                case ElementDefine.COMMAND.MP_DOWNLOAD:
                    {
                        ret = Download(ref msg, msg.sm.efusebindata, false);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
#if debug
                        Thread.Sleep(1000);
#endif
                        break;
                    }
                case ElementDefine.COMMAND.MP_READ_BACK_CHECK_PC:
                    {
                        ret = PowerOn();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;

                        ret = ReadBackCheck();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;

                        ret = PowerOff();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        break;
                    }
                case ElementDefine.COMMAND.MP_READ_BACK_CHECK:
                    {
                        ret = ReadBackCheck();
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        break;
                    }
                case ElementDefine.COMMAND.MP_BIN_FILE_CHECK:
                    {
                        string binFileName = msg.sub_task_json;

                        var blist = SharedAPI.LoadBinFileToList(binFileName);
                        if (blist.Count == 0)
                            ret = LibErrorCode.IDS_ERR_DEM_LOAD_BIN_FILE_ERROR;
                        else
                            ret = CheckBinData(blist);
                        break;
                    }
            }
            return ret;
        }
        #endregion
        private UInt32 FrozenBitCheck() //注意，这里没有把image里的Frozen bit置为1，记得在后面的流程中做这件事
        {
#if frozen
            return LibErrorCode.IDS_ERR_DEM_FROZEN;
#else
            SetWorkMode(ElementDefine.WORK_MODE.PROGRAM);
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            byte pval1 = 0, pval2 = 0;
            byte cfg = 0;
            ret = ReadByte((byte)ElementDefine.EF_CFG, ref cfg);
            ret = ReadByte((byte)ElementDefine.EF_USR_BANK1_TOP, ref pval1);
            ret = ReadByte((byte)ElementDefine.EF_USR_BANK2_TOP, ref pval2);

            if ((cfg & 0x80) == 0x80)
            {
                cfgFRZ = true;
            }
            else
                cfgFRZ = false;
            if ((pval1 & 0x80) == 0x80)
            {
                bank1FRZ = true;
            }
            else
                bank1FRZ = false;
            if ((pval2 & 0x80) == 0x80)
            {
                bank2FRZ = true;
            }
            else
                bank2FRZ = false;

            if (bank1FRZ && bank2FRZ)
            {
                return LibErrorCode.IDS_ERR_DEM_FROZEN;
            }

            return ret;
#endif
        }

        private UInt32 DirtyChipCheck()
        {
#if dirty
            return LibErrorCode.IDS_ERR_DEM_DIRTYCHIP;
#else
            SetWorkMode(ElementDefine.WORK_MODE.PROGRAM);
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            byte pval = 0;
            if (cfgFRZ == false)
            {
                ret = ReadByte((byte)ElementDefine.EF_CFG, ref pval);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    return ret;
                }
                else if (pval != 0)
                {
                    return LibErrorCode.IDS_ERR_DEM_DIRTYCHIP;
                }
            }
            if (bank1FRZ == false)
            {
                for (byte index = (byte)ElementDefine.EF_USR_BANK1_OFFSET; index <= (byte)ElementDefine.EF_USR_BANK1_TOP; index++)
                {
                    ret = ReadByte(index, ref pval);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        return ret;
                    }
                    else if (pval != 0)
                    {
                        return LibErrorCode.IDS_ERR_DEM_DIRTYCHIP;
                    }
                }
                return ret;
            }
            if (bank2FRZ == false)
            {
                for (byte index = (byte)ElementDefine.EF_USR_BANK2_OFFSET; index <= (byte)ElementDefine.EF_USR_BANK2_TOP; index++)
                {
                    ret = ReadByte(index, ref pval);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        return ret;
                    }
                    else if (pval != 0)
                    {
                        return LibErrorCode.IDS_ERR_DEM_DIRTYCHIP;
                    }
                }
                return ret;
            }
            return ret;
#endif
        }
        private UInt32 ReadBackCheck()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
#if readback
            return LibErrorCode.IDS_ERR_DEM_BUF_CHECK_FAIL;
#else
            SetWorkMode(ElementDefine.WORK_MODE.PROGRAM);
            byte pval = 0;
            for (byte badd = (byte)ElementDefine.EF_USR_BANK1_OFFSET; badd <= (byte)ElementDefine.EF_USR_BANK1_TOP; badd++)
            {
                ret = ReadByte((byte)(badd + 4 * WritingBank1Or2), ref pval);
                if (pval != EFUSEUSRbuf[badd - ElementDefine.EF_USR_BANK1_OFFSET])
                {
                    FolderMap.WriteFile("Read back check, address: 0x" + (badd + 4 * WritingBank1Or2).ToString("X2") + "\torigi value: 0x" + EFUSEUSRbuf[badd - ElementDefine.EF_USR_BANK1_OFFSET].ToString("X2") + "\tread value: 0x" + pval.ToString("X2"));
                    return LibErrorCode.IDS_ERR_DEM_BUF_CHECK_FAIL;
                }
            }
            if (cfgFRZ == false)
            {
                ret = ReadByte((byte)(0x16), ref pval);
                if (pval != EFUSEUSRbuf[4])
                {
                    return LibErrorCode.IDS_ERR_DEM_BUF_CHECK_FAIL;
                }
            }
            return ret;
#endif
        }
        public uint CheckBinData(List<byte> blist)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            int length = (ElementDefine.EF_USR_BANK1_TOP - ElementDefine.EF_USR_BANK1_OFFSET + 1) + 1;//多加一个1是因为0x16
            length *= 2;    //一个字节地址，一个字节数值
            if (blist.Count != length)
            {
                ret = LibErrorCode.IDS_ERR_DEM_BIN_LENGTH_ERROR;
            }
            else
            {
                if (blist[0] != ElementDefine.EF_CFG)
                {
                    ret = LibErrorCode.IDS_ERR_DEM_BIN_ADDRESS_ERROR;
                }
                else
                    for (int i = ElementDefine.EF_USR_BANK1_OFFSET, j = 1; i <= ElementDefine.EF_USR_BANK1_TOP; i++, j++)
                    {
                        if (blist[j * 2] != i)
                        {
                            ret = LibErrorCode.IDS_ERR_DEM_BIN_ADDRESS_ERROR;
                            break;
                        }
                    }
            }
            return ret;
        }

        private UInt32 Download(ref TASKMessage msg, List<byte> efusebindata, bool isWithPowerControl)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            ret = SetWorkMode(ElementDefine.WORK_MODE.PROGRAM);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                return ret;
            }
            if (isWithPowerControl)
            {
                ret = PowerOn();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;
            }
            LoadEFRegImgFromEFUSEBin(msg.sm.efusebindata);

            byte offset = 0;
            if (cfgFRZ == true)
            {
                //System.Windows.Forms.MessageBox.Show("Config register 0x16 is frozen. Skip to program it.");
                msg.gm.message = "Register 0x16 is frozen. Skip to program it.";
                msg.gm.level = 0;
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
            }
            else
            {
                byte address = 0x16;
                ret = WriteByte(address, EFUSEUSRbuf[4]);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    return ret;
                }
            }

            if (bank1FRZ == false)
            {
                //System.Windows.Forms.MessageBox.Show("Writing bank1.");
                msg.gm.message = "Writing bank1.";
                msg.gm.level = 0;
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                WritingBank1Or2 = 0;
            }
            else if (bank2FRZ == false)
            {
                offset = 4;
                //System.Windows.Forms.MessageBox.Show("Bank1 is frozen, writing bank2.");
                msg.gm.message = "Bank1 is frozen, writing bank2.";
                msg.gm.level = 0;
                msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                WritingBank1Or2 = 1;
            }

            for (byte badd = (byte)ElementDefine.EF_USR_BANK1_OFFSET; badd <= (byte)ElementDefine.EF_USR_BANK1_TOP; badd++)
            {
                ret = WriteByte((byte)(badd + offset), EFUSEUSRbuf[badd - ElementDefine.EF_USR_BANK1_OFFSET]);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    return ret;
                }
                byte tmp = 0;
                ret = ReadByte((byte)(badd + offset), ref tmp);     //Issue 1746 workaround
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    return ret;
                }
            }
            if (isWithPowerControl)
            {
                ret = PowerOff();
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;
            }

            ret = SetWorkMode(ElementDefine.WORK_MODE.NORMAL);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                return ret;
            }

            return ret;
        }
        private void LoadEFRegImgFromEFUSEBin(List<byte> efusebindata)
        {
            EFUSEUSRbuf[0] = efusebindata[3];
            EFUSEUSRbuf[1] = efusebindata[5];
            EFUSEUSRbuf[2] = efusebindata[7];
            EFUSEUSRbuf[3] = efusebindata[9];
            EFUSEUSRbuf[4] = efusebindata[1];
        }
    }
}
