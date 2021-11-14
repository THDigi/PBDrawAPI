using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Sandbox.ModAPI;

namespace Digi.PBDrawAPI
{
    public class APIHandler
    {
        public readonly string PropertyId;
        public bool Created { get; private set; }

        ImmutableDictionary<string, Delegate> Functions;
        ImmutableDictionary<string, Delegate>.Builder Builder;

        public APIHandler(string propertyId)
        {
            PropertyId = propertyId;
            Builder = ImmutableDictionary.CreateBuilder<string, Delegate>();
        }

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
            return Functions;
        }
    }
}
