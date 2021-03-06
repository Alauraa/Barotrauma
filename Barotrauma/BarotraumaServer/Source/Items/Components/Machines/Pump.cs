﻿using Barotrauma.Networking;
using Microsoft.Xna.Framework;
using System;
using System.Globalization;
using System.Xml.Linq;

namespace Barotrauma.Items.Components
{
    partial class Pump : Powered, IServerSerializable, IClientSerializable
    {
        public void ServerRead(ClientNetObject type, Lidgren.Network.NetBuffer msg, Client c)
        {
            float newFlowPercentage = msg.ReadRangedInteger(-10, 10) * 10.0f;
            bool newIsActive = msg.ReadBoolean();

            if (item.CanClientAccess(c))
            {
                if (newFlowPercentage != FlowPercentage)
                {
                    GameServer.Log(c.Character.LogName + " set the pumping speed of " + item.Name + " to " + (int)(newFlowPercentage) + " %", ServerLog.MessageType.ItemInteraction);
                }
                if (newIsActive != IsActive)
                {
                    GameServer.Log(c.Character.LogName + (newIsActive ? " turned on " : " turned off ") + item.Name, ServerLog.MessageType.ItemInteraction);
                }

                FlowPercentage = newFlowPercentage;
                IsActive = newIsActive;
            }

            //notify all clients of the changed state
            item.CreateServerEvent(this);
        }

        public void ServerWrite(Lidgren.Network.NetBuffer msg, Client c, object[] extraData = null)
        {
            //flowpercentage can only be adjusted at 10% intervals -> no need for more accuracy than this
            msg.WriteRangedInteger(-10, 10, (int)(flowPercentage / 10.0f));
            msg.Write(IsActive);
        }
    }
}
