using Cobra.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cobra.Woodpecker8
{
    internal class RegisterConfigDEMBehaviorManage : DEMBehaviorManageBase
    {
        #region 基础服务功能设计
        public override UInt32 Command(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            switch ((ElementDefine.COMMAND)msg.sub_task)
            {
                case ElementDefine.COMMAND.REGISTER_CONFIG_READ:
                    {
                        ParamContainer demparameterlist = msg.task_parameterlist;
                        if (demparameterlist == null) return ret;
                        if (msg.task_parameterlist.parameterlist.Count < ElementDefine.EF_TOTAL_PARAMS)
                            return ElementDefine.IDS_ERR_DEM_ONE_PARAM_DISABLE;
                        ret = GetRegisteInfor(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        msg.percent = 40;
                        ret = SetWorkMode(ElementDefine.WORK_MODE.WRITE_MAP_CTRL);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        msg.percent = 60;

                        byte offset = 0; 
                        if (isOPBank2Empty() == true)
                        {
                            offset = 0;
                            msg.gm.message = "Reading bank1.";
                            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                        }
                        else
                        {
                            offset = 4;
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
                        ret = SetWorkMode(ElementDefine.WORK_MODE.NORMAL);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        ret = ConvertHexToPhysical(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        break;
                    }
                case ElementDefine.COMMAND.REGISTER_CONFIG_WRITE:
                    {
                        ParamContainer demparameterlist = msg.task_parameterlist;
                        if (demparameterlist == null) return ret;
                        if (msg.task_parameterlist.parameterlist.Count < ElementDefine.EF_TOTAL_PARAMS)
                            return ElementDefine.IDS_ERR_DEM_ONE_PARAM_DISABLE;

                        msg.percent = 30;
                        ret = GetRegisteInfor(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        ret = SetWorkMode(ElementDefine.WORK_MODE.WRITE_MAP_CTRL);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        msg.percent = 60;

                        byte offset = 0;
                        List<byte> OpReglist = Utility.GenerateRegisterList(ref msg);
                        byte bdata = 0;
                        #region Read
                        if (isOPBank2Empty() == true)
                        {
                            offset = 0;
                            msg.gm.message = "Reading bank1.";
                            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                        }
                        else
                        {
                            offset = 4;
                            msg.gm.message = "Reading bank2.";
                            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                        }
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
                        #endregion
                        msg.percent = 50;
                        ret = ConvertPhysicalToHex(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        #region Write
                        ret = SafetyCheck(OpReglist);
                        if (isOPConfigEmpty() == false)
                        {
                            //System.Windows.Forms.MessageBox.Show("Config register 0x26 is frozen. Skip to program it.");
                            msg.gm.message = "Register 0x26 is frozen. Skip to program it.";
                            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                        }
                        if (isOPBank1Empty() == true)
                        {
                            offset = 0;
                            //System.Windows.Forms.MessageBox.Show("Writing bank1.");
                            msg.gm.message = "Writing bank1.";
                            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                        }
                        else if (isOPBank2Empty() == true)
                        {
                            offset = 4;
                            //System.Windows.Forms.MessageBox.Show("Bank1 is frozen, so writing OP registers is prohibited. Please check IC document for details.");
                            //return 0;
                            ret = ElementDefine.IDS_ERR_DEM_FROZEN_OP;
                            return ret;
                        }
                        else
                        {
                            //System.Windows.Forms.MessageBox.Show("Bank1 and bank2 are frozen, stop writing.");
                            //return 0;
                            return ElementDefine.IDS_ERR_DEM_FROZEN;
                        }
                        foreach (byte badd in OpReglist)
                        {
                            if (badd == 0x16 || badd == 0x26)
                            {
                                ret = WriteByte(badd, (byte)parent.m_OpRegImg[badd].val);
                            }
                            else
                                ret = WriteByte((byte)(badd + offset), (byte)parent.m_OpRegImg[badd].val);
                            parent.m_OpRegImg[badd].err = ret;
                        }
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        #endregion
                        #region Read
                        if (isOPBank2Empty() == true)
                        {
                            offset = 0;
                            msg.gm.message = "Reading bank1.";
                            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                        }
                        else
                        {
                            offset = 4;
                            msg.gm.message = "Reading bank2.";
                            msg.controlreq = COMMON_CONTROL.COMMON_CONTROL_WARNING;
                        }
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
                        #endregion
                        ret = ConvertHexToPhysical(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        ret = SetWorkMode(ElementDefine.WORK_MODE.NORMAL);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            return ret;
                        break;
                    }
            }
            return ret;
        }
        private bool isOPBank1Empty()
        {
            byte tmp = 0;
            ReadByte(0x2b, ref tmp);
            if ((tmp & 0x80) == 0x80)
                return false;
            else
                return true;
        }

        private bool isOPBank2Empty()
        {
            byte tmp = 0;
            ReadByte(0x2f, ref tmp);
            if ((tmp & 0x80) == 0x80)
                return false;
            else
                return true;
        }

        private bool isOPConfigEmpty()
        {
            byte tmp = 0;
            ReadByte(0x26, ref tmp);
            if ((tmp & 0x80) == 0x80)
                return false;
            else
                return true;
        }

        public UInt32 SafetyCheck(List<byte> OpReglist)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if ((parent.m_OpRegImg[ElementDefine.EF_USR_BANK2_TOP].val & 0x80) == 0x80
                && parent.m_OpRegImg[ElementDefine.EF_USR_BANK2_TOP].err == LibErrorCode.IDS_ERR_SUCCESSFUL
                && (parent.m_OpRegImg[ElementDefine.EF_USR_BANK1_TOP].val & 0x80) == 0x00
                && parent.m_OpRegImg[ElementDefine.EF_USR_BANK1_TOP].err == LibErrorCode.IDS_ERR_SUCCESSFUL)
            {
                ret = ElementDefine.IDS_ERR_DEM_BLOCK;
            }
            return ret;
        }
        #endregion
    }
}
