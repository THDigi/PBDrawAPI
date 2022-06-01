using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Entity;

namespace Digi.PBDebugAPI
{
    /// <summary>
    /// Creates the means for PB to interface with the mod, by adding a terminal property which sends a dictionary of delegates (functions).
    /// </summary>
    public class PBInterface
    {
        public readonly string PropertyId;
        public bool Created { get; private set; }

        private readonly PBDebugAPIMod Mod;

        ImmutableDictionary<string, Delegate> Functions;
        ImmutableDictionary<string, Delegate>.Builder Builder;

        public PBInterface(PBDebugAPIMod mod, string propertyId)
        {
            Mod = mod;
            PropertyId = propertyId;
            Builder = ImmutableDictionary.CreateBuilder<string, Delegate>();

            // need to add terminal controls/properties on first spawn of the block type we care about
            MyEntities.OnEntityCreate += EntityCreated;
        }

        public void Dispose()
        {
            MyEntities.OnEntityCreate -= EntityCreated;
        }

        public void AddMethod(string name, Delegate method)
        {
            if(Created)
                throw new Exception("Cannot add methods after API was already finalized.");

            Builder.Add(name, method);
        }

        void EntityCreated(MyEntity ent) // NOTE: called from a thread
        {
            IMyProgrammableBlock pb = ent as IMyProgrammableBlock;
            if(pb != null)
            {
                // only need the first PB
                MyEntities.OnEntityCreate -= EntityCreated;

                SubmitAndCreate();
            }
        }

        void SubmitAndCreate()
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
