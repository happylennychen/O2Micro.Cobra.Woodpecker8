using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using Cobra.Common;
using Cobra.Communication;

namespace Cobra.Woodpecker8
{
    public class DEMDeviceManage : IDEMLib
    {
        #region Properties

        internal double etrx
        {
            get
            {
                Parameter param = tempParamlist.GetParameterByGuid(ElementDefine.TpETRx);
                if (param == null) return 0.0;
                else return param.phydata;
            }
        }

        internal ParamContainer EFParamlist = null;
        internal ParamContainer OPParamlist = null;
        internal ParamContainer tempParamlist = null;

        internal BusOptions m_busoption = null;
        internal DeviceInfor m_deviceinfor = null;
        internal ParamListContainer m_Section_ParamlistContainer = null;
        internal ParamListContainer m_SFLs_ParamlistContainer = null;

        internal COBRA_HWMode_Reg[] m_OpRegImg = new COBRA_HWMode_Reg[ElementDefine.OP_MEMORY_SIZE];
        private Dictionary<UInt32, COBRA_HWMode_Reg[]> m_HwMode_RegList = new Dictionary<UInt32, COBRA_HWMode_Reg[]>();

        private DEMBehaviorManageBase m_dem_bm_base = new DEMBehaviorManageBase();
        private EFUSEConfigDEMBehaviorManage m_efuse_config_dem_bm = new EFUSEConfigDEMBehaviorManage();
        private RegisterConfigDEMBehaviorManage m_register_config_dem_bm = new RegisterConfigDEMBehaviorManage();
        private ExpertDEMBehaviorManage m_expert_dem_bm = new ExpertDEMBehaviorManage();
        private MassProductionDEMBehaviorManage m_mass_production_dem_bm = new MassProductionDEMBehaviorManage();

        public CCommunicateManager m_Interface = new CCommunicateManager();

        public Parameter pE_BAT_TYPE = new Parameter();
        public Parameter pE_DOT_TH = new Parameter();
        public Parameter pE_DOT_E = new Parameter();
        public Parameter pE_OVP_TH = new Parameter();
        public Parameter pE_UVP_TH = new Parameter();
        public Parameter pO_BAT_TYPE = new Parameter();
        public Parameter pO_DOT_TH = new Parameter();
        public Parameter pO_DOT_E = new Parameter();
        public Parameter pO_OVP_TH = new Parameter();
        public Parameter pO_UVP_TH = new Parameter();
        //public bool fromCFG = false;

        #endregion
        #region Dynamic ErrorCode
        public Dictionary<UInt32, string> m_dynamicErrorLib_dic = new Dictionary<uint, string>()
        {
            {ElementDefine.IDS_ERR_DEM_POWERON_FAILED,"Turn on programming voltage failed!"},
            {ElementDefine.IDS_ERR_DEM_POWEROFF_FAILED,"Turn off programming voltage failed!"},
            {ElementDefine.IDS_ERR_DEM_POWERCHECK_FAILED,"Programming voltage check failed!"},
            {ElementDefine.IDS_ERR_DEM_FROZEN,"Bank1 and bank2 are frozen, stop writing."},
            {ElementDefine.IDS_ERR_DEM_FROZEN_OP,"Bank1 is frozen, so writing OP registers is prohibited. Please check IC document for details."},
            {ElementDefine.IDS_ERR_DEM_BLOCK,"Bank2 frozen bit is set while bank1 frozen bit is not, the write operation is canceled."},
            {ElementDefine.IDS_ERR_DEM_ONE_PARAM_DISABLE,"Single parameter opeartion is not supported."},
        };
        #endregion
        #region other functions
        private void InitParameters()
        {
            ParamContainer pc = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.OperationElement);
            pE_DOT_TH = pc.GetParameterByGuid(ElementDefine.E_DOT_TH);
            pO_DOT_TH = pc.GetParameterByGuid(ElementDefine.O_DOT_TH);
            pE_BAT_TYPE = pc.GetParameterByGuid(ElementDefine.E_BAT_TYPE);
            pO_BAT_TYPE = pc.GetParameterByGuid(ElementDefine.O_BAT_TYPE);
            pE_OVP_TH = pc.GetParameterByGuid(ElementDefine.E_OVP_TH);
            pO_OVP_TH = pc.GetParameterByGuid(ElementDefine.O_OVP_TH);
            pE_UVP_TH = pc.GetParameterByGuid(ElementDefine.E_UVP_TH);
            pO_UVP_TH = pc.GetParameterByGuid(ElementDefine.O_UVP_TH);
            pc = m_Section_ParamlistContainer.GetParameterListByGuid(ElementDefine.VirtualElement);
            pE_DOT_E = pc.GetParameterByGuid(ElementDefine.E_DOT_E);
            pO_DOT_E = pc.GetParameterByGuid(ElementDefine.O_DOT_E);
        }

        private void SectionParameterListInit(ref ParamListContainer devicedescriptionlist)
        {
            tempParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.TemperatureElement);
            if (tempParamlist == null) return;

            OPParamlist = devicedescriptionlist.GetParameterListByGuid(ElementDefine.OperationElement);
            if (OPParamlist == null) return;
        }

        private void InitialImgReg()
        {
            for (byte i = 0; i < ElementDefine.OP_MEMORY_SIZE; i++)
            {
                m_OpRegImg[i] = new COBRA_HWMode_Reg();
                m_OpRegImg[i].val = ElementDefine.PARAM_HEX_ERROR;
                m_OpRegImg[i].err = LibErrorCode.IDS_ERR_BUS_DATA_PEC_ERROR;
            }
        }
        #endregion
        #region 接口实现
        public void Init(ref BusOptions busoptions, ref ParamListContainer deviceParamlistContainer, ref ParamListContainer sflParamlistContainer)
        {
            m_busoption = busoptions;
            m_Section_ParamlistContainer = deviceParamlistContainer;
            m_SFLs_ParamlistContainer = sflParamlistContainer;
            SectionParameterListInit(ref deviceParamlistContainer);

            //m_HwMode_RegList.Add(ElementDefine.EFUSEElement, m_EFRegImg);
            m_HwMode_RegList.Add(ElementDefine.OperationElement, m_OpRegImg);

            SharedAPI.ReBuildBusOptions(ref busoptions, ref deviceParamlistContainer);

            InitialImgReg();
            InitParameters();

            CreateInterface();

            m_dem_bm_base.parent = this;
            m_dem_bm_base.dem_dm = new DEMDataManageBase(m_dem_bm_base);
            m_register_config_dem_bm.parent = this;
            m_register_config_dem_bm.dem_dm = new DEMDataManageBase(m_register_config_dem_bm);
            m_efuse_config_dem_bm.parent = this;
            m_efuse_config_dem_bm.dem_dm = new DEMDataManageBase(m_efuse_config_dem_bm);//共用
            m_expert_dem_bm.parent = this;
            m_expert_dem_bm.dem_dm = new DEMDataManageBase(m_expert_dem_bm);
            m_mass_production_dem_bm.parent = this;
            m_mass_production_dem_bm.dem_dm = new DEMDataManageBase(m_mass_production_dem_bm);
            LibInfor.AssemblyRegister(Assembly.GetExecutingAssembly(), ASSEMBLY_TYPE.OCE); 
            LibErrorCode.UpdateDynamicalLibError(ref m_dynamicErrorLib_dic);

        }

        public bool EnumerateInterface()
        {
            return m_Interface.FindDevices(ref m_busoption);
        }

        public bool CreateInterface()
        {
            bool bdevice = EnumerateInterface();
            if (!bdevice) return false;

            return m_Interface.OpenDevice(ref m_busoption);
        }

        public bool DestroyInterface()
        {
            return m_Interface.CloseDevice();
        }
        public void UpdataDEMParameterList(Parameter p)
        {
            if ((p.guid & 0x00001000) == 0x00001000)
                m_efuse_config_dem_bm.dem_dm.UpdateEpParamItemList(p);
            else if ((p.guid & 0x00002000) == 0x00002000)
                m_register_config_dem_bm.dem_dm.UpdateEpParamItemList(p);
        }

        public UInt32 GetDeviceInfor(ref DeviceInfor deviceinfor)
        {
#if debug
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
#else
            return m_dem_bm_base.GetDeviceInfor(ref deviceinfor);
#endif
        }

        public UInt32 Erase(ref TASKMessage bgworker)
        {
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        public UInt32 BlockMap(ref TASKMessage bgworker)
        {
                return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }

        public UInt32 Command(ref TASKMessage bgworker)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            switch ((ElementDefine.COMMAND)bgworker.sub_task)
            {
                case ElementDefine.COMMAND.REGISTER_CONFIG_WRITE:
                case ElementDefine.COMMAND.REGISTER_CONFIG_READ:
                    {
                        ret = m_register_config_dem_bm.Command(ref bgworker);
                        break;
                    }
                case ElementDefine.COMMAND.EFUSE_CONFIG_WRITE:
                case ElementDefine.COMMAND.EFUSE_CONFIG_READ:
                case ElementDefine.COMMAND.EFUSE_CONFIG_SAVE_EFUSE_HEX:
                    {
                        ret = m_efuse_config_dem_bm.Command(ref bgworker);
                        break;
                    }
                case ElementDefine.COMMAND.MP_BIN_FILE_CHECK:
                case ElementDefine.COMMAND.MP_DIRTY_CHIP_CHECK:
                case ElementDefine.COMMAND.MP_DIRTY_CHIP_CHECK_PC:
                case ElementDefine.COMMAND.MP_DOWNLOAD:
                case ElementDefine.COMMAND.MP_DOWNLOAD_PC:
                case ElementDefine.COMMAND.MP_FROZEN_BIT_CHECK:
                case ElementDefine.COMMAND.MP_FROZEN_BIT_CHECK_PC:
                case ElementDefine.COMMAND.MP_READ_BACK_CHECK:
                case ElementDefine.COMMAND.MP_READ_BACK_CHECK_PC:
                    {
                        ret = m_mass_production_dem_bm.Command(ref bgworker);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            m_mass_production_dem_bm.PowerOff();
                        break;
                    }
            }
            return ret;
        }

        public UInt32 Read(ref TASKMessage bgworker)
        {
            return m_dem_bm_base.Read(ref bgworker);
        }

        public UInt32 Write(ref TASKMessage bgworker)
        {
            return m_dem_bm_base.Write(ref bgworker);
        }

        public UInt32 BitOperation(ref TASKMessage bgworker)
        {
            return m_dem_bm_base.BitOperation(ref bgworker);
        }

        public UInt32 ConvertHexToPhysical(ref TASKMessage bgworker)
        {
            //if (bgworker.gm.sflname == "Expert")
            //    return m_expert_dem_bm.ConvertHexToPhysical(ref bgworker);
            //else if (bgworker.gm.sflname == "Register Config")
            //    return m_register_config_dem_bm.ConvertHexToPhysical(ref bgworker);
            //else if (bgworker.gm.sflname == "EFUSE Config")
            //    return m_efuse_config_dem_bm.ConvertHexToPhysical(ref bgworker);
            //else
                return m_dem_bm_base.ConvertHexToPhysical(ref bgworker);
        }

        public UInt32 ConvertPhysicalToHex(ref TASKMessage bgworker)
        {
            if (bgworker.gm.sflname == "Expert")
                return m_expert_dem_bm.ConvertPhysicalToHex(ref bgworker);
            else if (bgworker.gm.sflname == "Register Config")
                return m_register_config_dem_bm.ConvertPhysicalToHex(ref bgworker);
            else if (bgworker.gm.sflname == "EFUSE Config")
                return m_efuse_config_dem_bm.ConvertPhysicalToHex(ref bgworker);
            else
                return m_dem_bm_base.ConvertPhysicalToHex(ref bgworker);
        }

        public UInt32 GetSystemInfor(ref TASKMessage bgworker)
        {
#if debug
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
#endif
            return m_dem_bm_base.GetSystemInfor(ref bgworker);
        }

        public UInt32 GetRegisteInfor(ref TASKMessage bgworker)
        {
#if debug
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
#endif
            return m_dem_bm_base.GetRegisteInfor(ref bgworker);
        }
        #endregion
    }
}

