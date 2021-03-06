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
using System.Linq;
using OpenRA.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	[Desc("Requires `GpsWatcher` on the player actor.")]
	class GpsPowerInfo : SupportPowerInfo
	{
		public readonly int RevealDelay = 0;

		public readonly string DoorImage = "atek";
		[SequenceReference("DoorImage")] public readonly string DoorSequence = "active";
		[PaletteReference] public readonly string DoorPalette = "effect";

		public readonly string SatelliteImage = "sputnik";
		[SequenceReference("SatelliteImage")] public readonly string SatelliteSequence = "idle";
		[PaletteReference] public readonly string SatellitePalette = "effect";

		public override object Create(ActorInitializer init) { return new GpsPower(init.Self, this); }
	}

	class GpsPower : SupportPower, INotifyKilled, INotifySold, INotifyOwnerChanged
	{
		readonly GpsPowerInfo info;
		GpsWatcher owner;

		public GpsPower(Actor self, GpsPowerInfo info)
			: base(self, info)
		{
			this.info = info;
			owner = self.Owner.PlayerActor.Trait<GpsWatcher>();
			owner.GpsAdd(self);
		}

		public override void Charged(Actor self, string key)
		{
			self.Owner.PlayerActor.Trait<SupportPowerManager>().Powers[key].Activate(new Order());
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			self.World.AddFrameEndTask(w =>
			{
				Game.Sound.PlayToPlayer(self.Owner, Info.LaunchSound);

				w.Add(new SatelliteLaunch(self, info));

				owner.Launch(self, info);
			});
		}

		public void Killed(Actor self, AttackInfo e) { RemoveGps(self); }

		public void Selling(Actor self) { }
		public void Sold(Actor self) { RemoveGps(self); }

		void RemoveGps(Actor self)
		{
			// Extra function just in case something needs to be added later
			owner.GpsRemove(self);
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			RemoveGps(self);
			owner = newOwner.PlayerActor.Trait<GpsWatcher>();
			owner.GpsAdd(self);
		}
	}
}
