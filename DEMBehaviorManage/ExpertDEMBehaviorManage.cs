﻿using Cobra.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cobra.Woodpecker8
{
    internal class ExpertDEMBehaviorManage : DEMBehaviorManageBase
    {

        #region 基础服务功能设计
        public override UInt32 Command(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            switch ((ElementDefine.COMMAND)msg.sub_task)
            {
                case ElementDefine.COMMAND.EXPERT_PROGRAM_MODE:
                    ret = SetWorkMode(ElementDefine.WORK_MODE.PROGRAM);
                    break;
                case ElementDefine.COMMAND.EXPERT_NORMAL_MODE:
                    ret = SetWorkMode(ElementDefine.WORK_MODE.NORMAL);
                    break;
                default:

                    break;
            }
            return ret;
        }
        #endregion
    }
}
