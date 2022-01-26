using Cobra.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cobra.Woodpecker8
{
    public class RegisterConfigDEMDataManage : DEMDataManageBase
    {
        bool FromHexToPhy = false;
        public RegisterConfigDEMDataManage(object pParent) : base(pParent)
        {
        }
        /// <summary>
        /// 更新参数ItemList
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        /// <returns></returns>
        public override void UpdateEpParamItemList(Parameter pTarget)
        {
            if (pTarget.errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                return;
            Parameter source = new Parameter();
            switch (pTarget.guid)
            {
                case ElementDefine.O_DOT_E:
                    UpdateDOTE(ref pTarget);
                    break;
                case ElementDefine.O_OVP_TH:
                    UpdateOVP(ref pTarget);
                    break;
                case ElementDefine.O_OVR_HYS:
                    UpdateOVR(ref pTarget);
                    break;
                case ElementDefine.O_UVR_HYS:
                    UpdateUVR(ref pTarget);
                    break;
            }
            FromHexToPhy = false;
            return;
        }

        public override void Physical2Hex(ref Parameter p)
        {
            UInt16 wdata = 0;
            double dtmp = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.SUBTYPE)p.subtype)
            {
                case ElementDefine.SUBTYPE.OVP:
                    dtmp = p.phydata - p.offset;
                    wdata = (UInt16)((double)(dtmp * p.regref) / (double)p.phyref);
                    ret = WriteToRegImg(p, wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        WriteToRegImgError(p, ret);
                    break;
                case ElementDefine.SUBTYPE.DOT_TH:
                    Parameter pDOT_E = parent.parent.pO_DOT_E;
                    if (pDOT_E.phydata == 1)    //Disable
                    {
                        wdata = 0;
                    }
                    else if (pDOT_E.phydata == 0)   //Enable
                    {
                        wdata = (ushort)(p.phydata + 2);
                    }
                    ret = WriteToRegImg(p, wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        WriteToRegImgError(p, ret);
                    break;
                default:
                    dtmp = p.phydata - p.offset;
                    wdata = (UInt16)((double)(dtmp * p.regref) / (double)p.phyref);
                    ret = WriteToRegImg(p, wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        WriteToRegImgError(p, ret);
                    break;
            }
        }

        /// <summary>
        /// 转换参数值类型从物理值到16进制值
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        public override void Hex2Physical(ref Parameter p)
        {
            UInt16 wdata = 0;
            double dtmp = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.SUBTYPE)p.subtype)
            {
                case ElementDefine.SUBTYPE.DOT_TH:
                    ret = ReadFromRegImg(p, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    if (wdata >= 2)
                        p.phydata = wdata - 2;
                    else
                        p.phydata = 0;
                    break;
                case ElementDefine.SUBTYPE.OVP:
                    ret = ReadFromRegImg(p, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    if (wdata < 0x0a)
                        wdata = 0x0a;
                    else if (wdata > 0x3c)
                        wdata = 0x3c;
                    dtmp = (double)((double)wdata * p.phyref / p.regref);
                    p.phydata = dtmp + p.offset;
                    break;
                default:
                    ret = ReadFromRegImg(p, ref wdata);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    {
                        p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                        break;
                    }
                    dtmp = (double)((double)wdata * p.phyref / p.regref);
                    p.phydata = dtmp + p.offset;
                    break;
            }
            FromHexToPhy = true;
        }
        private void UpdateDOTE(ref Parameter pDOT_E)
        {
            Parameter pDOT = parent.parent.pO_DOT_TH;

            if (pDOT_E.phydata == 1)                     //pDOT_E.phydata是1的情况下，DOT变化了，那肯定是在读芯片, pDOT.hexdata已经是准确的了
            {
                if (pDOT.hexdata < 2)
                {
                    pDOT_E.phydata = 1;
                }
                else
                {
                    pDOT_E.phydata = 0;
                }
            }
            else if (pDOT_E.phydata == 0)               //pDOT_E.phydata是0的情况下，DOT变化了，有可能是读芯片，也可能是UI操作
            {
                //如果是读芯片，那么就还是直接使用hexdata
                if (FromHexToPhy)
                {
                    if (pDOT.hexdata < 2)
                    {
                        pDOT_E.phydata = 1;
                    }
                    else
                    {
                        pDOT_E.phydata = 0;
                    }
                }
                //*/
                //如果是UI操作，那么就什么都不用做
            }
        }
        private void UpdateOVP(ref Parameter pOVP_TH)
        {
            Parameter pBAT_TYPE = parent.parent.pO_BAT_TYPE;
            if (pBAT_TYPE.phydata == 0)
            {
                pOVP_TH.offset = 3900;
                pOVP_TH.dbPhyMin = 4000;
                pOVP_TH.dbPhyMax = 4500;
                if (pOVP_TH.phydata < 4000)
                    pOVP_TH.phydata = 4000;
            }
            else if (pBAT_TYPE.phydata == 1)
            {
                pOVP_TH.offset = 3400;
                pOVP_TH.dbPhyMin = 3500;
                pOVP_TH.dbPhyMax = 4000;
                if (pOVP_TH.phydata > 4000)
                    pOVP_TH.phydata = 4000;
            }
        }
        private void UpdateOVR(ref Parameter pOVR)
        {
            Parameter pBAT_TYPE = parent.parent.pO_BAT_TYPE;
            Parameter pOVP = parent.parent.pO_OVP_TH;
            if (pBAT_TYPE.phydata == 0)
            {
                if (pOVP.phydata >= 4050)
                {
                    if (!pOVR.itemlist.Contains("400mV"))
                    {
                        pOVR.itemlist.Add("400mV");
                    }
                }
                else
                {
                    if (pOVR.itemlist.Contains("400mV"))
                    {
                        pOVR.itemlist.Remove("400mV");
                    }
                }

            }
            else if (pBAT_TYPE.phydata == 1)
            {
                if (pOVP.phydata >= 3550)
                {
                    if (!pOVR.itemlist.Contains("400mV"))
                    {
                        pOVR.itemlist.Add("400mV");
                    }
                }
                else
                {
                    if (pOVR.itemlist.Contains("400mV"))
                    {
                        pOVR.itemlist.Remove("400mV");
                    }
                }
            }
        }
        private void UpdateUVR(ref Parameter pUVR)
        {
            Parameter pUVP = parent.parent.pO_UVP_TH;
            int num = 16 - (int)pUVP.phydata;
            if (num > 8)
                num = 8;
            int diff = pUVR.itemlist.Count - num;
            if (diff > 0)
            {
                for (int i = diff; i > 0; i--)
                {
                    pUVR.itemlist.RemoveAt(pUVR.itemlist.Count - 1);
                }
            }
            else if (diff < 0)
            {
                for (int i = -diff; i > 0; i--)
                {
                    pUVR.itemlist.Add(((pUVR.itemlist.Count + 1) * 100).ToString() + "mV");
                }
            }
        }
    }
}
