using Iced.Intel;
using MichaelWeaponsMod.Content.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Animations;
using Terraria.ID;
using Terraria.ModLoader;

namespace MichaelWeaponsMod.Content.Projectiles
{
    public class WispProjectileAdittional : ModProjectile
    {
        private const string GreenPath = "MichaelWeaponsMod/Assets/WispProjectileGreen";
        private static Asset<Texture2D> GreenTexture;
        public override void Load()
        {
            GreenTexture = ModContent.Request<Texture2D>(GreenPath);
        }
        public override void SetStaticDefaults()
        {
            // Total count animation frames
            Main.projFrames[Projectile.type] = 4;
        }
        public override void SetDefaults()
        {
            Projectile.width = 10; // The width of projectile hitbox
            Projectile.height = 16; // The height of projectile hitbox

            Projectile.DamageType = DamageClass.Default; // Is the projectile shoot by a ranged weapon?
            Projectile.ignoreWater = true; // Does the projectile's speed be influenced by water?
            Projectile.tileCollide = false; // Can the projectile collide with tiles?
            Projectile.penetrate = -1; // Look at comments ExamplePiercingProjectile
            Projectile.timeLeft = 600;
            Projectile.netImportant = true; // This ensures that the projectile is synced when other players join the world.
        }
        public enum WispType
        {
            Ourple,
            Green,
        }
        // Store the wisp type in the Projectile.ai array, which is synced by vanilla for us :-D
        public WispType ThisWisp
        {
            get => (WispType)(int)Projectile.ai[1];
            set => Projectile.ai[1] = (int)value;
        }
        public enum WispState
        {
            Orbiting,
            Chasing
        }
        // Store the wisp type in the Projectile.ai array, which is synced by vanilla for us :-D
        public WispState ThisState
        {
            get => (WispState)(int)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }
        public float Radius = 210f;
        public int dustTime = 10;
        public static int Rotation = 0;
        public override void AI()
        {
            var player = Main.player[Projectile.owner];
            int dustType;
            int allowed;
            switch (ThisWisp)
            {
                case WispType.Green:
                    dustType = DustID.GreenTorch;
                    allowed = 1;
                    break;
                default:
                    dustType = DustID.PurpleTorch;
                    allowed = 2;
                    break;
            }

            if (Projectile.timeLeft < 500)
            {
                var Buffs = player.GetModPlayer<WispPlayer>().Buffs;
                foreach (var member in Buffs)
                {
                    if (member.Value.Num == (int)Projectile.ai[2])
                    {
                        CustomCollisionLogic(Projectile.getRect(), allowed, Buffs, member);
                        break;
                    }
                }
            }

            //Getting the total amount of orbiting projectiles of this type
            int total = 0;
            foreach (var proj in Main.ActiveProjectiles)
            {
                if (proj.type == Projectile.type) {
                    if ((int)proj.ai[0] == 0)
                        total++; }
            }

            //Handling dust spawn
            dustTime--;
            if (dustTime <= 0)
            {
                dustTime = 10;
                int num = Projectile.timeLeft < 599 ? 1 : 3;
                for (int i = 0; i < num; i++)
                {
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, 0, 0);
                }
            }

            //Getting the closest target ("allowed" is to only search for targets taht are NPCs/Players, accordingly)
            var target = new Vector2(0, 0);
            target = Closest(Projectile.Center, 220 * 220, allowed);

            //Handling movement
            if (target == new Vector2(0, 0) || Projectile.timeLeft > 500) {
                Projectile.velocity = OrbitMovement(player.Center, Projectile.Center, Radius, total, (int)Projectile.ai[2]);
                ThisState = WispState.Orbiting; }
            else {
                Projectile.velocity = Projectile.Center.DirectionTo(target);
                ThisState = WispState.Chasing; }

            //Handling animation
            if (++Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                // Or more compactly Projectile.frame = ++Projectile.frame % Main.projFrames[Projectile.type];
                if (++Projectile.frame >= Main.projFrames[Projectile.type])
                    Projectile.frame = 0;
            }

            //Handling rotation for the orbit (in degrees). Here, I'm using a static property instead of Projectile.ai[],
            //because I need to sync all of the projectiles of this type
            //and they have a dynamic amount, which doesn't leave me much choice.
            //I'm also checking if its the "first" projectile of this type,
            //to avoid every single projectile of this type meddling with the static rotation.
            if ((int)Projectile.ai[2] == 1)
                Rotation++;
            if (Rotation > 360)
                Rotation -= 360;
        }
        public override void OnKill(int timeLeft)
        {
            foreach (var proj in Main.ActiveProjectiles)
            {
                if (proj.type == Projectile.type)
                {
                    //Updating the "order" of the projectiles of this type whenever one dies
                    if ((int)proj.ai[2] > (int)Projectile.ai[2])
                        proj.ai[2]--;
                }
            }
        }
        public Vector2 OrbitMovement(Vector2 center, Vector2 position, float radius, int total, int num)
        {
            var velocity = new Vector2(0, 0);

            // Calculate the current rotation in radians
            float currentRotation = (MathHelper.TwoPi / 360) * Rotation + (MathHelper.TwoPi / total) * num;

            // Get the offset from the center using the rotation and radius
            Vector2 offset = currentRotation.ToRotationVector2() * radius;
            velocity = position.DirectionTo(center + offset) * position.Distance(center + offset);
            return velocity;
        }
        public Vector2 Closest(Vector2 position, float rangeSQ, int allowed)
        {
            //allowed = 1 for only players, 2 for only npc's, 0 for both
            var target = new Vector2(0, 0);
            if (allowed != 1)
            {
                foreach (var npc in Main.npc)
                {
                    if (position.DistanceSQ(npc.Center) < rangeSQ && npc.active && npc.CanBeChasedBy())
                    {
                        target = npc.Center;
                        rangeSQ = position.DistanceSQ(npc.Center);
                    }
                }
            }
            if (allowed != 2)
            {
                foreach (var player in Main.player)
                {
                    if (position.DistanceSQ(player.Center) < rangeSQ && player.active)
                    {
                        target = player.Center;
                        rangeSQ = position.DistanceSQ(player.Center);
                    }
                }
            }

            return target;
        }
        public void CustomCollisionLogic(Rectangle projRect, int allowed, Dictionary<int, WispPlayer.TwoInts> dict, KeyValuePair<int, WispPlayer.TwoInts> member)
        {
            var type = member.Key;
            var time = member.Value.Time;
            //allowed = 1 for only players, 2 for only npc's, 0 for both
            if (allowed != 1)
            {
                int index = Array.FindIndex(Main.npc, target => Projectile.Colliding(projRect, target.getRect()) && target.active);
                if (index != -1) {
                    Main.npc[index].AddBuff(type, time);
                    dict.Remove(type);
                    Projectile.Kill(); }
            }
            if (allowed != 2)
            {
                int index = Array.FindIndex(Main.player, target => Projectile.Colliding(projRect, target.getRect()) && target.active);
                if (index != -1)
                {
                    Main.player[index].AddBuff(type, time);
                    dict.Remove(type);
                    Projectile.Kill();
                }
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            Texture2D texture;

            // Getting texture of projectile
            switch (ThisWisp)
            {
                case WispType.Green:
                    texture = GreenTexture.Value;
                    break;
                default:
                    texture = TextureAssets.Projectile[Type].Value;
                    break;
            }

            // Calculating frameHeight and current Y pos dependence of frame
            // If texture without animation frameHeight is always texture.Height and startY is always 0
            int frameHeight = texture.Height / Main.projFrames[Type];
            int startY = frameHeight * Projectile.frame;

            // Get this frame on texture
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);

            Vector2 origin = sourceRectangle.Size() / 2f;

            // Applying lighting and draw current frame
            Color drawColor = Projectile.GetAlpha(lightColor);
            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                sourceRectangle, drawColor, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);

            // It's important to return false, otherwise we also draw the original texture.
            return false;
        }
    }

    public class WispProjectile : ModProjectile
    {
        private const string RedPath = "MichaelWeaponsMod/Assets/WispProjectileRed";
        private const string BluePath = "MichaelWeaponsMod/Assets/WispProjectileBlue";

        private static Asset<Texture2D> RedTexture;
        private static Asset<Texture2D> BlueTexture;

        public override void Load()
        {
            RedTexture = ModContent.Request<Texture2D>(RedPath);
            BlueTexture = ModContent.Request<Texture2D>(BluePath);
        }

        public override void SetStaticDefaults()
        {
            // Total count animation frames
            Main.projFrames[Projectile.type] = 4;
        }
        public override void SetDefaults()
        {
            Projectile.width = 10; // The width of projectile hitbox
            Projectile.height = 16; // The height of projectile hitbox

            Projectile.DamageType = DamageClass.Default; // Is the projectile shoot by a ranged weapon?
            Projectile.ignoreWater = true; // Does the projectile's speed be influenced by water?
            Projectile.tileCollide = false; // Can the projectile collide with tiles?
            Projectile.penetrate = -1; // Look at comments ExamplePiercingProjectile
            Projectile.timeLeft = 2;
            Projectile.netImportant = true; // This ensures that the projectile is synced when other players join the world.
        }
        // Allows you to determine the color and transparency in which a projectile is drawn
        // Return null to use the default color (normally light and buff color)
        // Returns null by default.
        public enum WispType
        {
            White,
            None,
            Red,
            Blue,
        }
        // Store the wisp type in the Projectile.ai array, which is synced by vanilla for us :-D
        public WispType ThisWisp
        {
            get => (WispType)(int)Projectile.ai[1];
            set => Projectile.ai[1] = (int)value;
        }

        public float Radius = 70f;
        public int time = 120;
        public int dustTime = 10;
        public WispType PreviousType = WispType.White;
        public override void AI()
        {
            var player = Main.player[Projectile.owner];

            foreach (var equip in player.armor)
            {
                if (player.active && player.statLife != 0 && equip.type == ModContent.ItemType<WispBox>()) {
                    Projectile.timeLeft++;
                    break; }
            }

            bool collFriend = false;
            if (Projectile.Colliding(Projectile.getRect(), player.getRect()))
                collFriend = true;

            int dustType = DustID.WhiteTorch;

            switch (ThisWisp)
            {
                case WispType.Red:
                    dustType = DustID.RedTorch;
                    Radius -= 0.2f;
                    if (collFriend)
                    {
                        player.Heal(10);
                        ThisWisp = WispType.None;
                    }
                    break;
                case WispType.Blue:
                    dustType = DustID.BlueFlare;
                    time--;
                    if (time <= 0)
                    {
                        var target = Closest(Projectile.Center, 700, 2);

                        if (time <= -300)
                        {
                            time = 120;
                            ThisWisp = WispType.None;
                        }

                        if (target == new Vector2(0, 0))
                            break;

                        var vel = Projectile.Center.DirectionTo(target) * 10;
                        Projectile.NewProjectile(player.GetSource_FromThis(), Projectile.Center, vel, ProjectileID.Bullet, 10, 5, Projectile.owner, 0, 0, 0);
                        ThisWisp = WispType.None;
                        time = 120;
                    }
                    break;
                default:
                    if (Radius < 70f)
                        Radius += 0.2f;
                    else
                    {
                        if (ThisWisp == WispType.None)
                        {
                            time--;
                            if (time <= -120)
                                ThisWisp = WispType.White;
                        }
                        else
                            time = 120;
                    }
                    break;
            }

            dustTime--;
            if (dustTime <= 0)
            {
                dustTime = 10;
                int num = PreviousType == ThisWisp ? 1 : 3;
                for (int i = 0; i < num; i++)
                {
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, 0, 0);
                }
            }

            Projectile.ai[0]--;
            if (Projectile.ai[0] < -360)
                Projectile.ai[0] += 360;

            if (++Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                // Or more compactly Projectile.frame = ++Projectile.frame % Main.projFrames[Projectile.type];
                if (++Projectile.frame >= Main.projFrames[Projectile.type])
                    Projectile.frame = 0;
            }

            // Calculate the current rotation in radians
            float currentRotation = (MathHelper.TwoPi / 360) * Projectile.ai[0] + (MathHelper.TwoPi / 3) * Projectile.ai[2];

            // Get the offset from the center using the rotation and radius
            Vector2 offset = currentRotation.ToRotationVector2() * Radius;
            Projectile.velocity = Projectile.Center.DirectionTo(player.MountedCenter + offset) * Projectile.Center.Distance(player.MountedCenter + offset);

            PreviousType = ThisWisp;
        }
        public Vector2 Closest(Vector2 position, float range, int allowed)
        {
            //allowed = 1 for only players, 2 for only npc's, 0 for both
            var target = new Vector2(0, 0);
            if (allowed != 1)
            {
                foreach (var npc in Main.npc)
                {
                    if (position.Distance(npc.Center) < range && npc.active && npc.CanBeChasedBy())
                    {
                        target = npc.Center;
                        range = position.Distance(npc.Center);
                    }
                }
            }
            if (allowed != 2)
            {
                foreach (var player in Main.player)
                {
                    if (position.Distance(player.Center) < range && player.active)
                    {
                        target = player.Center;
                        range = position.Distance(player.Center);
                    }
                }
            }

            return target;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            Texture2D texture;

            // Getting texture of projectile
            switch (ThisWisp)
            {
                case WispType.Red:
                    texture = RedTexture.Value;
                    break;
                case WispType.Blue:
                    texture = BlueTexture.Value;
                    break;
                default:
                    texture = TextureAssets.Projectile[Type].Value;
                    break;
            }

            // Calculating frameHeight and current Y pos dependence of frame
            // If texture without animation frameHeight is always texture.Height and startY is always 0
            int frameHeight = texture.Height / Main.projFrames[Type];
            int startY = frameHeight * Projectile.frame;

            // Get this frame on texture
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);

            Vector2 origin = sourceRectangle.Size() / 2f;

            // Applying lighting and draw current frame
            Color drawColor = Projectile.GetAlpha(lightColor);
            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                sourceRectangle, drawColor, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);

            // It's important to return false, otherwise we also draw the original texture.
            return false;
        }
    }
}