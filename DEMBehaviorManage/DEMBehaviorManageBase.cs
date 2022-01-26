﻿using Cobra.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Cobra.Woodpecker8
{
    class DEMBehaviorManageBase
    {
        //父对象保存
        private DEMDeviceManage m_parent;
        public DEMDeviceManage parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }
        public DEMDataManageBase dem_dm { get; set; }
        private object m_lock = new object();
        #region 操作寄存器操作
        #region 操作寄存器父级操作
        protected UInt32 ReadByte(byte reg, ref byte pval)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnReadByte(reg, ref pval);
            }
            return ret;
        }

        protected UInt32 WriteByte(byte reg, byte val)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnWriteByte(reg, val);
            }
            return ret;
        }

        protected UInt32 PowerOn()
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnPowerOn();
            }
            return ret;
        }
        protected UInt32 PowerOff()
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnPowerOff();
            }
            return ret;
        }

        protected UInt32 SetWorkMode(ElementDefine.WORK_MODE wkm)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnSetWorkMode(wkm);
            }
            return ret;
        }
        protected UInt32 GetWorkMode(ref ElementDefine.WORK_MODE wkm)
        {
            UInt32 ret = 0;
            lock (m_lock)
            {
                ret = OnGetWorkMode(ref wkm);
            }
            return ret;
        }

        #endregion

        #region 操作寄存器子级操作
        protected byte crc8_calc(ref byte[] pdata, UInt16 n)
        {
            byte crc = 0;
            byte crcdata;
            UInt16 i, j;

            for (i = 0; i < n; i++)
            {
                crcdata = pdata[i];
                for (j = 0x80; j != 0; j >>= 1)
                {
                    if ((crc & 0x80) != 0)
                    {
                        crc <<= 1;
                        crc ^= 0x07;
                    }
                    else
                        crc <<= 1;

                    if ((crcdata & j) != 0)
                        crc ^= 0x07;
                }
            }
            return crc;
        }

        protected byte calc_crc_read(byte slave_addr, byte reg_addr, byte data)
        {
            byte[] pdata = new byte[5];

            pdata[0] = slave_addr;
            pdata[1] = reg_addr;
            pdata[2] = (byte)(slave_addr | 0x01);
            pdata[3] = data;

            return crc8_calc(ref pdata, 4);
        }

        protected byte calc_crc_write(byte slave_addr, byte reg_addr, byte data)
        {
            byte[] pdata = new byte[4];

            pdata[0] = slave_addr; ;
            pdata[1] = reg_addr;
            pdata[2] = data;

            return crc8_calc(ref pdata, 3);
        }

        protected UInt32 OnReadByte(byte reg, ref byte pval)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[2];
            byte[] receivebuf = new byte[2];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
                sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = reg;

            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (parent.m_Interface.ReadDevice(sendbuf, ref receivebuf, ref DataOutLen, 2))
                {
                    if (receivebuf[1] != calc_crc_read(sendbuf[0], sendbuf[1], receivebuf[0]))
                    {
                        pval = ElementDefine.PARAM_HEX_ERROR;
                        ret = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
                    }
                    else
                    {
                        pval = receivebuf[0];
                        ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    }
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }

            //m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }

        protected UInt32 OnWriteByte(byte reg, byte val)
        {
            UInt16 DataOutLen = 0;
            byte[] sendbuf = new byte[4];
            byte[] receivebuf = new byte[1];
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            try
            {
                sendbuf[0] = (byte)parent.m_busoption.GetOptionsByGuid(BusOptions.I2CAddress_GUID).SelectLocation.Code;
            }
            catch (System.Exception ex)
            {
                return ret = LibErrorCode.IDS_ERR_DEM_LOST_PARAMETER;
            }
            sendbuf[1] = reg;
            sendbuf[2] = val;

            sendbuf[3] = calc_crc_write(sendbuf[0], sendbuf[1], sendbuf[2]);
            for (int i = 0; i < ElementDefine.RETRY_COUNTER; i++)
            {
                if (parent.m_Interface.WriteDevice(sendbuf, ref receivebuf, ref DataOutLen, 2))
                {
                    ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
                    break;
                }
                ret = LibErrorCode.IDS_ERR_DEM_FUN_TIMEOUT;
                Thread.Sleep(10);
            }

            //m_Interface.GetLastErrorCode(ref ret);
            return ret;
        }

        protected UInt32 OnGetWorkMode(ref ElementDefine.WORK_MODE wkm)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            byte buf = 0;
            ret = OnReadByte(ElementDefine.WORKMODE_OFFSET, ref buf);
            buf &= 0x03;
            wkm = (ElementDefine.WORK_MODE)buf;
            return ret;
        }

        protected UInt32 OnSetWorkMode(ElementDefine.WORK_MODE wkm)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            byte buf = 0;
            ret = OnReadByte(ElementDefine.WORKMODE_OFFSET, ref buf);
            buf &= 0xfc;
            buf |= (byte)wkm;
            buf |= 0xA0;
            ret = OnWriteByte(ElementDefine.WORKMODE_OFFSET, buf);
            ret = OnWriteByte(ElementDefine.WORKMODE_OFFSET, buf);
            return ret;
        }

        protected UInt32 OnGetAllowWrite(ref bool allow_write)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            byte buf = 0;
            ret = OnReadByte(ElementDefine.WORKMODE_OFFSET, ref buf);
            allow_write = (buf & 0x80) == 0x80;
            return ret;
        }

        protected UInt32 OnSetAllowWrite(bool allow_write)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            byte buf = 0;
            ret = OnReadByte(ElementDefine.WORKMODE_OFFSET, ref buf);
            if (allow_write)
                buf |= 0x80;
            else
                buf &= 0x7f;
            ret = OnWriteByte(ElementDefine.WORKMODE_OFFSET, buf);
            return ret;
        }

        protected UInt32 OnGetMappingDisable(ref bool mapping_disable)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            byte buf = 0;
            ret = OnReadByte(ElementDefine.MAPPINGDISABLE_OFFSET, ref buf);
            mapping_disable = (buf & 0x20) == 0x20;
            return ret;
        }

        protected UInt32 OnSetMappingDisable(bool mapping_disable)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            byte buf = 0;
            ret = OnReadByte(ElementDefine.MAPPINGDISABLE_OFFSET, ref buf);
            if (mapping_disable)
                buf |= 0x20;
            else
                buf &= 0xdf;
            ret = OnWriteByte(ElementDefine.MAPPINGDISABLE_OFFSET, buf);
            return ret;
        }

        private UInt32 OnPowerOn()
        {
            byte[] yDataIn = { 0x51 };
            byte[] yDataOut = { 0, 0 };
            ushort uOutLength = 2;
            ushort uWrite = 1;
            if (parent.m_Interface.SendCommandtoAdapter(yDataIn, ref yDataOut, ref uOutLength, uWrite))
            {
                if (uOutLength == 2 && yDataOut[0] == 0x51 && yDataOut[1] == 0x1)
                {
                    Thread.Sleep(200);	//sync with SP8G2
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;
                }
                else
                    return ElementDefine.IDS_ERR_DEM_POWERON_FAILED;
            }
            return ElementDefine.IDS_ERR_DEM_POWERON_FAILED;
        }

        private UInt32 OnPowerOff()
        {
            byte[] yDataIn = { 0x52 };
            byte[] yDataOut = { 0, 0 };
            ushort uOutLength = 2;
            ushort uWrite = 1;
            if (parent.m_Interface.SendCommandtoAdapter(yDataIn, ref yDataOut, ref uOutLength, uWrite))
            {
                if (uOutLength == 2 && yDataOut[0] == 0x52 && yDataOut[1] == 0x2)
                {
                    return LibErrorCode.IDS_ERR_SUCCESSFUL;
                }
                else
                    return ElementDefine.IDS_ERR_DEM_POWEROFF_FAILED;
            }
            return ElementDefine.IDS_ERR_DEM_POWEROFF_FAILED;
        }
        #endregion
        #endregion
        #region 基础服务功能设计

        public virtual UInt32 Read(ref TASKMessage msg)
        {
            byte bdata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            List<byte> OpReglist = new List<byte>();

            ParamContainer demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;

            OpReglist = SharedAPI.GenerateRegisterList(ref msg);
            if (OpReglist == null)
                return ret;

            foreach (byte badd in OpReglist)
            {
                ret = ReadByte(badd, ref bdata);
                parent.m_OpRegImg[badd].err = ret;
                parent.m_OpRegImg[badd].val = bdata;
                //if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                //    return ret;
            }
            return ret;
        }

        public virtual UInt32 Write(ref TASKMessage msg)    //Expert 把这里污染了 //去掉了污染20200513
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            List<byte> OpReglist = new List<byte>();

            OpReglist = SharedAPI.GenerateRegisterList(ref msg);
            if (OpReglist == null)
                return ret;
            foreach (byte badd in OpReglist)
            {
                ret = WriteByte(badd, (byte)parent.m_OpRegImg[badd].val);
                parent.m_OpRegImg[badd].err = ret;
                //if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                //    return ret;
            }

            return ret;
        }

        public UInt32 BitOperation(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }

        public UInt32 ConvertHexToPhysical(ref TASKMessage msg) //Scan 把这里污染了
        {
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> OpParamList = new List<Parameter>();
            OpParamList = SharedAPI.GenerateParameterList(ref msg);
            if (OpParamList == null)
                return ret;

            for (int i = 0; i < OpParamList.Count; i++)
            {
                param = (Parameter)OpParamList[i];
                if (param == null) continue;
                dem_dm.Hex2Physical(ref param);
            }

            return ret;
        }

        public UInt32 ConvertPhysicalToHex(ref TASKMessage msg)
        {
            Parameter param = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            List<Parameter> OpParamList = new List<Parameter>();
            OpParamList = SharedAPI.GenerateParameterList(ref msg);
            if (OpParamList == null)
                return ret;

            for (int i = 0; i < OpParamList.Count; i++)
            {
                param = (Parameter)OpParamList[i];
                if (param == null) continue;
                if ((param.guid & ElementDefine.SectionMask) == ElementDefine.TemperatureElement) continue;

                dem_dm.Physical2Hex(ref param);
            }

            return ret;
        }

        public virtual UInt32 Command(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }
        public UInt32 EpBlockRead()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            return ret;
        }
        #endregion

        #region 特殊服务功能设计
        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            byte pval1 = 0, pval2 = 0;
            ret = ReadByte(0x00, ref pval1);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;
            ret = ReadByte(0x01, ref pval2);
            if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

            if (pval1 != 0x57 || pval2 != 0x08)
                return LibErrorCode.IDS_ERR_DEM_BETWEEN_SELECT_BOARD;

            deviceinfor.status = 0;
            deviceinfor.type = pval1 << 8 | pval2;

            return ret;
        }

        public virtual UInt32 GetSystemInfor(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            return ret;
        }

        public UInt32 GetRegisteInfor(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            return ret;
        }
        #endregion
    }
}
