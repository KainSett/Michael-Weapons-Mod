using MichaelWeaponsMod.Content.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        public static bool DoExist = true;
        public override void SetStaticDefaults()
        {
            // Total count animation frames
            Main.projFrames[Projectile.type] = 5;
        }
        public override void SetDefaults()
        {
            Projectile.width = 10; // The width of projectile hitbox
            Projectile.height = 10; // The height of projectile hitbox

            Projectile.friendly = true; // Can the projectile deal damage to enemies?
            Projectile.DamageType = DamageClass.Default; // Is the projectile shoot by a ranged weapon?
            Projectile.ignoreWater = true; // Does the projectile's speed be influenced by water?
            Projectile.tileCollide = false; // Can the projectile collide with tiles?
            Projectile.penetrate = -1; // Look at comments ExamplePiercingProjectile
            Projectile.timeLeft = 2;
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
            Purple
        }
        // Store the wisp type in the Projectile.ai array, which is synced by vanilla for us :-D
        public WispType ThisWisp
        {
            get => (WispType)(int)Projectile.ai[1];
            set => Projectile.ai[1] = (int)value;
        }
    
    public override Color? GetAlpha(Color lightColor)
        {
            switch (ThisWisp)
            {
                case WispType.Red: return new Color(Color.IndianRed.R, Color.IndianRed.G, Color.IndianRed.B, lightColor.A);
                case WispType.Blue: return new Color(Color.LightSkyBlue.R, Color.LightSkyBlue.G, Color.LightSkyBlue.B, lightColor.A);
                case WispType.Purple: return new Color(Color.Purple.R, Color.Purple.G, Color.Purple.B, lightColor.A);
                default: return lightColor;
            }
        }
        public float Radius = 70f;
        public int count = 0;
        public int time = 120;
        public override void AI()
        {
            var player = Main.player[Projectile.owner];
            foreach (var equip in player.armor)
            {
                if (equip.type == ModContent.ItemType<WispBox>())
                    Projectile.timeLeft++;
            }

            bool collFriend = false;
            if (Projectile.Colliding(Projectile.getRect(), player.getRect()))
                collFriend = true;

            switch (ThisWisp)
            {
                case WispType.Red:
                    Radius -= 0.2f;
                    if (collFriend) {
                        player.Heal(10);
                        ThisWisp = WispType.None; }
                    break;
                case WispType.Blue:
                    time--;
                    if (time <= 0)
                    {
                        time = 120;
                        Projectile.NewProjectile(player.GetSource_FromThis(), Projectile.Center, player.Center, ProjectileID.Bullet, 10, 10, Projectile.owner, 0, 0, 0);
                        ThisWisp = WispType.None;
                    }
                    break;
                case WispType.Purple:
                    if (collFriend) {
                        player.AddBuff(BuffID.Confused, 180);
                        ThisWisp = WispType.None; }
                    time--;
                    if (time <= -600)
                        ThisWisp = WispType.None;
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
            

            Projectile.ai[0]++;
            if (Projectile.ai[0] > 360)
                Projectile.ai[0] -= 360;

            if (++Projectile.frameCounter >= 4)
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
            Projectile.Center = player.MountedCenter + offset;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            switch (ThisWisp)
            {
                case WispType.Purple:
                    target.AddBuff(BuffID.Venom, 180);
                    ThisWisp = WispType.None;
                    break;
                case WispType.Red:
                    target.life += (int)MathHelper.Min(10, target.lifeMax - target.life - 1);
                    break;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;

            // Getting texture of projectile
            Texture2D texture = TextureAssets.Projectile[Type].Value;

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