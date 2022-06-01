using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Sandbox.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities;
using IMyProgrammableBlock = Sandbox.ModAPI.Ingame.IMyProgrammableBlock;

namespace Digi.PBDebugAPI
{
    public class ChatCommands : IDisposable
    {
        const string MainCommand = "/pbdraw";

        readonly PBDebugAPIMod Mod;

        public ChatCommands(PBDebugAPIMod mod)
        {
            Mod = mod;

            MyAPIGateway.Utilities.MessageEntered += MessageEntered;
        }

        public void Dispose()
        {
            MyAPIGateway.Utilities.MessageEntered -= MessageEntered;
        }

        void MessageEntered(string messageText, ref bool sendToOthers)
        {
            try
            {
                if(!messageText.StartsWith(MainCommand))
                    return;

                sendToOthers = false;

                TextPtr cmd = new TextPtr(messageText, MainCommand.Length);
                cmd = cmd.SkipWhitespace();

                if(cmd.StartsWithCaseInsensitive("clear"))
                {
                    try
                    {
                        foreach(var kv in Mod.DrawPerPB)
                        {
                            IMyProgrammableBlock pb = kv.Key;
                            DebugObjectHost handler = kv.Value;

                            foreach(DebugObjectBase drawObject in handler.Objects.Values)
                            {
                                drawObject.Dispose();
                            }

                            handler.Objects.Clear();
                        }
                    }
                    finally
                    {
                        Mod.DrawPerPB.Clear();
                    }

                    MyAPIGateway.Utilities.ShowMessage(Log.ModName, "cleared all objects.");
                    return;
                }

                MyAPIGateway.Utilities.ShowMessage(Log.ModName, "Available commands:");
                MyAPIGateway.Utilities.ShowMessage($"{MainCommand} clear ", "removes all objects from all PBs");
            }
            catch(Exception e)
            {
                Log.Error(e);
            }
        }
    }
}
