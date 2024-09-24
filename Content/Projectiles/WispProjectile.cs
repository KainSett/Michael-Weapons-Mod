using MichaelWeaponsMod.Content.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
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
    public class WispProjectile : ModProjectile
    {
        private const string RedPath = "MichaelWeaponsMod/Assets/WispProjectileRed";
        private const string OurplePath = "MichaelWeaponsMod/Assets/WispProjectilePurple";
        private const string BluePath = "MichaelWeaponsMod/Assets/WispProjectileBlue";

        private static Asset<Texture2D> RedTexture;
        private static Asset<Texture2D> OurpleTexture;
        private static Asset<Texture2D> BlueTexture;

        public override void Load()
        {
            RedTexture = ModContent.Request<Texture2D>(RedPath);
            OurpleTexture = ModContent.Request<Texture2D>(OurplePath);
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
            Projectile.timeLeft = 600;
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
            Purple,
            Green
        }
        // Store the wisp type in the Projectile.ai array, which is synced by vanilla for us :-D
        public WispType ThisWisp
        {
            get => (WispType)(int)Projectile.ai[1];
            set => Projectile.ai[1] = (int)value;
        }
        
        public float Radius = 70f;
        public int count = 0;
        public int time = 120;
        public int dustTime = 10;
        public WispType PreviousType = WispType.White;
        public override void AI()
        {
            bool IsDefault = ThisWisp != WispType.Green && ThisWisp != WispType.Purple;

            var player = Main.player[Projectile.owner];
            if (IsDefault)
            {
                foreach (var equip in player.armor)
                {
                    if (player.active && player.statLife != 0 && equip.type == ModContent.ItemType<WispBox>())
                        Projectile.timeLeft = 2;
                }
            }

            var total = 0;
            foreach (var proj in Main.projectile)
            {
                if (Main.player[proj.owner] == player && proj.type == ModContent.ProjectileType<WispProjectile>())
                {
                    total++;
                }
            }

            bool collFriend = false;
            bool collHostile = false;
            var NPCtarget = new NPC();
            foreach (var npc in Main.ActiveNPCs)
            {
                if (!npc.friendly && Projectile.Colliding(Projectile.getRect(), npc.getRect()))
                {
                    collHostile = true;
                    NPCtarget = npc;
                }
            }
            if (Projectile.Colliding(Projectile.getRect(), player.getRect()))
                collFriend = true;

            int dustType = DustID.WhiteTorch;

            switch (ThisWisp)
            {
                case WispType.Red:
                    dustType = DustID.RedTorch;
                    Radius -= 0.2f;
                    if (collFriend) {
                        player.Heal(10);
                        ThisWisp = WispType.None; }
                    break;
                case WispType.Blue:
                    dustType = DustID.BlueFlare;
                    time--;
                    if (time <= 0)
                    {
                        var target = Closest(Projectile.Center, 700, 2);

                        if (time <= -300) {
                            time = 120;
                            ThisWisp = WispType.None; }

                        if (target == new Vector2(0, 0))
                            break;

                        var vel = Projectile.Center.DirectionTo(target) * 10;
                        Projectile.NewProjectile(player.GetSource_FromThis(), Projectile.Center, vel, ProjectileID.Bullet, 10, 5, Projectile.owner, 0, 0, 0);
                        ThisWisp = WispType.None;
                        time = 120;
                    }
                    break;
                case WispType.Purple:
                    dustType = DustID.PurpleCrystalShard;

                    break;
                case WispType.Green:
                    dustType = DustID.GreenTorch;

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
                for (int i = 0; i < num; i++) { 
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, 0, 0); }
            }

            if (IsDefault) {
                Projectile.ai[0]--;
                if (Projectile.ai[0] < -360)
                    Projectile.ai[0] += 360; }
            else {
                Projectile.ai[0]++;
                if (Projectile.ai[0] > 360)
                    Projectile.ai[0] -= 360; }

            if (++Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                // Or more compactly Projectile.frame = ++Projectile.frame % Main.projFrames[Projectile.type];
                if (++Projectile.frame >= Main.projFrames[Projectile.type])
                    Projectile.frame = 0;
            }

            // Calculate the current rotation in radians
            var place = IsDefault ? (MathHelper.TwoPi / 3) * Projectile.ai[2] : (MathHelper.TwoPi / (total - 3)) * (Projectile.ai[2] - 3);
            float currentRotation = (MathHelper.TwoPi / 360) * Projectile.ai[0] + place;

            // Get the offset from the center using the rotation and radius
            if (!IsDefault)
                Radius = 150;
            Vector2 offset = currentRotation.ToRotationVector2() * Radius;
            Projectile.velocity = Projectile.Center.DirectionTo(player.MountedCenter + offset) * Projectile.Center.Distance(player.MountedCenter + offset);
            
            if (!IsDefault)
            {
                var target = ThisWisp == WispType.Purple ? Closest(Projectile.Center, 600, 2) : Closest(Projectile.Center, 600, 1);
                if (target != new Vector2(0, 0))
                    Projectile.velocity = Projectile.Center.DirectionTo(target);
            }

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
                case WispType.Purple:
                    texture = OurpleTexture.Value;
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