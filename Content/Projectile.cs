using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Mono.Cecil;
using Terraria.DataStructures;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using static MichaelWeaponsMod.Content.MichaelArrow;

namespace MichaelWeaponsMod.Content
{
    public class RectangleToTriangles
    {
        public Vector2 TopLeft { get; }
        public Vector2 TopRight { get; }
        public Vector2 BottomLeft { get; }
        public Vector2 BottomRight { get; }
        public Vector2 Center { get; }

        public RectangleToTriangles(Vector2 topLeft, int width, int height)
        {
            TopLeft = topLeft;
            TopRight = new Vector2(topLeft.X + width, topLeft.Y);
            BottomLeft = new Vector2(topLeft.X, topLeft.Y + height);
            BottomRight = new Vector2(topLeft.X + width, topLeft.Y + height);
            Center = new Vector2(topLeft.X + width / 2f, topLeft.Y + height / 2f);
        }

        public (Vector2, Vector2, Vector2)[] GetTriangles()
        {
            return new (Vector2, Vector2, Vector2)[]
            {
            (TopLeft, TopRight, Center),
            (TopRight, BottomRight, Center),
            (BottomRight, BottomLeft, Center),
            (BottomLeft, TopLeft, Center)
            };
        }

        public int? GetContainingTriangleIndex(Vector2 point)
        {
            var triangles = GetTriangles();

            for (int i = 0; i < triangles.Length; i++)
            {
                var (A, B, C) = triangles[i];

                if (IsPointInTriangle(point, A, B, C))
                {
                    return i; // Return the index of the triangle containing the point
                }
            }

            return null; // Point is not in any triangle
        }

        private bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            // Compute vectors
            var v0 = c - a;
            var v1 = b - a;
            var v2 = p - a;

            // Compute dot products
            float dot00 = Vector2.Dot(v0, v0);
            float dot01 = Vector2.Dot(v0, v1);
            float dot02 = Vector2.Dot(v0, v2);
            float dot11 = Vector2.Dot(v1, v1);
            float dot12 = Vector2.Dot(v1, v2);

            // Compute barycentric coordinates
            float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
            float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            // Check if point is in triangle
            return (u >= 0) && (v >= 0) && (u + v <= 1);
        }
    }
    public class MichaelsBow : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 1;
            Item.DamageType = DamageClass.Ranged;
            Item.knockBack = 2;
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.shootSpeed = 1;
            Item.width = 20;
            Item.height = 50;
            Item.noMelee = true;
        }
        public override bool? UseItem(Player player)
        {
            var position = CalculateSpawn();
            var velocity = position.DirectionTo(player.Center) * 10;
            Projectile.NewProjectile(player.GetSource_ItemUse(Item), position, velocity, ModContent.ProjectileType<MichaelArrow>(), Item.damage, Item.knockBack, Main.myPlayer, 0, 0, 0);
            return true;
        }
        public Vector2 CalculateSpawn()
        {
            var rectangle = new RectangleToTriangles(Main.screenPosition, Main.screenWidth, Main.screenHeight);
            Vector2 position = new Vector2(0, 0);
            Vector2 point = new Vector2(Main.screenPosition.X + Main.mouseX, Main.screenPosition.Y + Main.mouseY);
            int? triangleIndex = rectangle.GetContainingTriangleIndex(point);
            float x = position.X;
            float y = position.Y;
            switch (triangleIndex)
            {
                case 0: x = point.X; y = Main.screenPosition.Y; break;
                case 1: x = Main.screenPosition.X + Main.screenWidth;  y = point.Y; break;
                case 2: x = point.X; y = Main.screenPosition.Y + Main.screenHeight; break;
                case 3: x = Main.screenPosition.X; y = point.Y; break;
            }
            return position = new Vector2(x, y);
        }
    }
    public class MichaelArrow : ModProjectile
    {
        private const string AimTexturePath = "MichaelWeaponsMod/Content/AimTexture"; // The folder path to the flail chain sprite

        private static Asset<Texture2D> aimTexture;
        public override void Load()
        {
            aimTexture = ModContent.Request<Texture2D>(AimTexturePath);
        }
        // Store the target NPC using Projectile.ai[0]
        private NPC HomingTarget;
        public enum AiState
        {
            Wandering,
            Chasing
        }
        public AiState CurrentAIState
        {
            get => (AiState)Projectile.ai[0];
            set => Projectile.ai[0] = (float)value;
        }
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true; // Make the cultist resistant to this projectile, as it's resistant to all homing projectiles.
        }

        // Setting the default parameters of the projectile
        // You can check most of Fields and Properties here https://github.com/tModLoader/tModLoader/wiki/Projectile-Class-Documentation
        public override void SetDefaults()
        {
            Projectile.width = 32; // The width of projectile hitbox
            Projectile.height = 14; // The height of projectile hitbox

            Projectile.aiStyle = 0; // The ai style of the projectile (0 means custom AI). For more please reference the source code of Terraria
            Projectile.friendly = true; // Can the projectile deal damage to enemies?
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true; // Does the projectile's speed be influenced by water?
            Projectile.light = 1f; // How much light emit around the projectile
            Projectile.timeLeft = 6000; // The live time for the projectile (60 = 1 second, so 600 is 10 seconds)
            Projectile.maxPenetrate = -1;
            Projectile.penetrate = -1;
        }
        public override void AI()
        {
            CurrentAIState = AiState.Wandering;
            float maxDistance = 120;

            Projectile.rotation = Projectile.velocity.ToRotation();
            // First, we find a homing target if we don't have one
            if (HomingTarget == null)
            {
                HomingTarget = FindClosestNPC(maxDistance);
            }

            // If we have a homing target, make sure it is still valid. If the NPC dies or moves away, we'll want to find a new target
            if (HomingTarget != null && !IsValidTarget(HomingTarget))
            {
                HomingTarget = null;
            }

            if (HomingTarget == null)
            {
                return;
            }
            CurrentAIState = AiState.Chasing;

            var target = HomingTarget.Center;
            // If found, we rotate the projectile velocity in the direction of the target.
            // We only rotate by 3 degrees an update to give it a smooth trajectory. Increase the rotation speed here to make tighter turns
            float length = Projectile.velocity.Length();
            float targetAngle = Projectile.AngleTo(target);
            Projectile.velocity = Projectile.velocity.ToRotation().AngleTowards(targetAngle, MathHelper.ToRadians(9)).ToRotationVector2() * length;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        // Finding the closest NPC to attack within maxDetectDistance range
        // If not found then returns null
        public NPC FindClosestNPC(float maxDetectDistance)
        {
            NPC closestNPC = null;

            // Using squared values in distance checks will let us skip square root calculations, drastically improving this method's speed.
            float sqrMaxDetectDistance = maxDetectDistance * maxDetectDistance;

            // Loop through all NPCs
            foreach (var target in Main.ActiveNPCs)
            {
                // Check if NPC able to be targeted. 
                if (IsValidTarget(target))
                {
                    // The DistanceSquared function returns a squared distance between 2 points, skipping relatively expensive square root calculations
                    float sqrDistanceToTarget = Vector2.DistanceSquared(target.Center, Projectile.Center);

                    // Check if it is within the radius
                    if (sqrDistanceToTarget < sqrMaxDetectDistance)
                    {
                        sqrMaxDetectDistance = sqrDistanceToTarget;
                        closestNPC = target;
                    }
                }
            }
            return closestNPC;
        }

        public bool IsValidTarget(NPC target)
        {
            // This method checks that the NPC is:
            // 1. active (alive)
            // 2. chaseable (e.g. not a cultist archer)
            // 3. max life bigger than 5 (e.g. not a critter)
            // 4. can take damage (e.g. moonlord core after all it's parts are downed)
            // 5. hostile (!friendly)
            // 6. not immortal (e.g. not a target dummy)
            // 7. doesn't have solid tiles blocking a line of sight between the projectile and NPC
            return target.CanBeChasedBy();
        }
        public float rot = 0f;
        public override bool PreDrawExtras()
        {
            var textureToDraw = aimTexture;// your texture here;
            Vector2 center = Projectile.Center;// your center position here;
            float radius = 120f; // The radius of the circle
            int segments = 30; // The number of segments (points) in the circle
                               // The origin of the texture (center point of the texture)
            Vector2 origin = new Vector2(textureToDraw.Width() / 2f, textureToDraw.Height() / 2f);
            if (CurrentAIState == AiState.Wandering)
            {
                rot += 0.04f;

                // Loop through each segment to draw the texture at each point around the circle
                for (int i = 0; i < segments; ++i)
                {
                    // Calculate the current rotation in radians
                    float currentRotation = (MathHelper.TwoPi / segments) * i + rot;

                    // Get the offset from the center using the rotation and radius
                    Vector2 offset = currentRotation.ToRotationVector2() * radius;

                    // Calculate the draw position by adding the offset to the center
                    Vector2 drawPosition = center + offset;

                    // Draw the texture at the calculated position
                    Main.spriteBatch.Draw(
                        textureToDraw.Value,
                        drawPosition - Main.screenPosition, // Adjust for screen position
                        null, // Source rectangle (null means the whole texture)
                        Color.White, // The color to draw the texture (can be modified)
                        currentRotation, // Rotation (optional, you can set it to 0 if not needed)
                        origin, // Origin point (center of the texture)
                        1f, // Scale
                        SpriteEffects.None, // Effects (e.g., flipping)
                        0f // Layer depth
                    );
                }
                return true;
            }
            return false;
        }
    }
}
