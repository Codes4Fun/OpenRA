#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Commands;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
    [Desc("Displays domain index above the tile.")]
    class RenderDomainIndexInfo : ITraitInfo
    {
        public readonly Color Color = Color.White;
        public readonly string Font = "TinyBold";

        public object Create(ActorInitializer init) { return new RenderDomainIndex(init.Self, this); }
    }

    class RenderDomainIndex : IWorldLoaded, IChatCommand, IRender
    {
        const string CommandDebugDomains = "debugdomains";
        const string CommandDebugDesc = "Enables the domain index debug overlay. Takes movement class argument.";
        const string CommandListClasses = "listmovementclasses";
        const string CommandListDesc = "List movement classes for rendering domains.";

        public bool Enabled;

        readonly SpriteFont font;
        readonly Color color;

        DomainIndex domainIndex;
        IEnumerable<uint> movementClasses;
        uint movementClass;

        public RenderDomainIndex(Actor self, RenderDomainIndexInfo info)
        {
            color = info.Color;
            font = Game.Renderer.Fonts[info.Font];
        }

        public void WorldLoaded(World w, WorldRenderer wr)
        {
            domainIndex = w.WorldActor.TraitOrDefault<DomainIndex>();
            if (domainIndex == null)
                return;

            movementClasses =
                w.Map.Rules.Actors.Where(ai => ai.Value.HasTraitInfo<MobileInfo>())
                .Select(ai => (uint)ai.Value.TraitInfo<MobileInfo>().GetMovementClass(w.TileSet)).Distinct();

            var console = w.WorldActor.TraitOrDefault<ChatCommands>();
            var help = w.WorldActor.TraitOrDefault<HelpCommand>();

            if (console == null || help == null)
                return;

            console.RegisterCommand(CommandDebugDomains, this);
            help.RegisterHelp(CommandDebugDomains, CommandDebugDesc);

            console.RegisterCommand(CommandListClasses, this);
            help.RegisterHelp(CommandListClasses, CommandListDesc);
        }

        public void InvokeCommand(string name, string arg)
        {
            if (name == CommandDebugDomains)
            {
                Enabled = uint.TryParse(arg, out movementClass) && movementClasses.Contains(movementClass);
            }
            else if (name == CommandListClasses)
            {
    			foreach (var mc in movementClasses)
	    			Game.Debug("{0}", mc);
            }
        }

        public IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
        {
            if (!Enabled)
                yield break;

            foreach (var uv in wr.Viewport.VisibleCellsInsideBounds.CandidateMapCoords)
            {
                var cell = uv.ToCPos(wr.World.Map);
                var center = wr.World.Map.CenterOfCell(cell);
                var strength = domainIndex.GetIndex(cell, movementClass);

                yield return new TextRenderable(font, center, 0, color, strength.ToString());
            }
        }
    }
}
