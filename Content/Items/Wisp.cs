using MichaelWeaponsMod.Content.Projectiles;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static MichaelWeaponsMod.Content.Projectiles.WispProjectile;

namespace MichaelWeaponsMod.Content.Items
{
    public class WispBox : ModItem
    {
        public override void SetDefaults()
        {
            Item.accessory = true;
            Item.height = 20;
            Item.width = 20;
        }
        public override void UpdateEquip(Player player)
        {
            bool DoExist = true;
            foreach (var proj in Main.ActiveProjectiles)
            {
                if (Main.player[proj.owner] == player && proj.type == ModContent.ProjectileType<WispProjectile>())
                {
                    DoExist = false;
                }
            }
            if (DoExist)
            {
                Projectile.NewProjectile(player.GetSource_Accessory(Item), player.MountedCenter, new Microsoft.Xna.Framework.Vector2(0, 0), ModContent.ProjectileType<WispProjectile>(), 0, 0, player.whoAmI, 0, 0, 1);
                Projectile.NewProjectile(player.GetSource_Accessory(Item), player.MountedCenter, new Microsoft.Xna.Framework.Vector2(0, 0), ModContent.ProjectileType<WispProjectile>(), 0, 0, player.whoAmI, 0, 0, 2);
                Projectile.NewProjectile(player.GetSource_Accessory(Item), player.MountedCenter, new Microsoft.Xna.Framework.Vector2(0, 0), ModContent.ProjectileType<WispProjectile>(), 0, 0, player.whoAmI, 0, 0, 3);
            }
        }
    }
    public class WispPlayer : ModPlayer
    {
        public override void PostHurt(Player.HurtInfo info)
        {
            LevelUpWisp(WispType.Red);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo info, int damaeDone)
        {
            LevelUpWisp(WispType.Blue);
        }
        public override void Load()
        {
            Terraria.On_Player.AddBuff_DetermineBuffTimeToAdd += WispBuffCheck;
        }

        private static int WispBuffCheck(On_Player.orig_AddBuff_DetermineBuffTimeToAdd orig, Player self, int type, int time1)
        {
            int buffTime = orig(self, type, time1);
            if (time1 <= 3)
                return time1;

            int color;
            if (Main.debuff[type])
                color = 4;
            else
                color = 5;

            var num = 1;
            foreach (var proj in Main.ActiveProjectiles)
            {
                if (Main.player[proj.owner] == self && proj.type == ModContent.ProjectileType<WispProjectile>())
                    num++;
            }
            Projectile.NewProjectile(self.GetSource_FromThis(), self.MountedCenter, new Microsoft.Xna.Framework.Vector2(0, 0), ModContent.ProjectileType<WispProjectile>(), 0, 0, self.whoAmI, 0, color, num);
            return time1;
        }
        public WispProjectile LevelUpWisp(WispType type)
        {
            if (Player.ownedProjectileCounts[ModContent.ProjectileType<WispProjectile>()] == 0)
            {
                // There are no wisp projectiles
                return null;
            }

            // Find the wisp projectile with the lowest colour
            WispProjectile lowestWispProj = null;
            foreach (var proj in Main.ActiveProjectiles)
            {
                if (proj.ModProjectile is WispProjectile wispProj && proj.owner == Player.whoAmI && (lowestWispProj == null || wispProj.ThisWisp < lowestWispProj.ThisWisp))
                {
                    lowestWispProj = wispProj;
                }
            }

            if (lowestWispProj == null || lowestWispProj.ThisWisp != WispType.White)
            {
                // Could not find a wisp projectile or none can be leveled up
                return null;
            }

            // Level up the wisp by increasing its colour
            lowestWispProj.ThisWisp = type;
            lowestWispProj.Projectile.netUpdate = true;

            return lowestWispProj;
        }
    }
}