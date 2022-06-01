using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Sandbox.ModAPI;
using VRage;

namespace Digi.PBDebugAPI
{
    public class APIHandler
    {
        public readonly string PropertyId;
        public bool Created { get; private set; }

        private readonly PBDebugMod Mod;

        ImmutableDictionary<string, Delegate> Functions;
        ImmutableDictionary<string, Delegate>.Builder Builder;

        public APIHandler(PBDebugMod mod, string propertyId)
        {
            Mod = mod;
            PropertyId = propertyId;
            Builder = ImmutableDictionary.CreateBuilder<string, Delegate>();
        }

        public void Dispose() { }

        public void AddMethod(string name, Delegate method)
        {
            if(Created)
                throw new Exception("Cannot add methods after it was submited.");

            Builder.Add(name, method);
        }

        public void SubmitAndCreate()
        {
            if(Created)
                return;

            Created = true;
            Functions = Builder.ToImmutable();
            Builder = null;

            var p = MyAPIGateway.TerminalControls.CreateProperty<IReadOnlyDictionary<string, Delegate>, IMyProgrammableBlock>(PropertyId);
            p.Getter = Getter;
            p.Setter = (b, v) => { };
            MyAPIGateway.TerminalControls.AddControl<IMyProgrammableBlock>(p);
        }

        IReadOnlyDictionary<string, Delegate> Getter(IMyTerminalBlock block)
        {
            if(Functions == null)
                throw new Exception("API was not generated yet... a PB needs to exist first.");

            IMyProgrammableBlock pb = block as IMyProgrammableBlock;
            if(pb == null)
                throw new Exception("The API can only be retrieved from a PB");

            Mod.VerifyAPI(pb);

            return Functions;
        }
    }
}
