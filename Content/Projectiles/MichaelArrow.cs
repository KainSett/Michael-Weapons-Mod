using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using MichaelWeaponsMod.Content.NPCs.NPCDictionaryStuff;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MichaelWeaponsMod.Content.Projectiles
{
    public class MichaelArrow : ModProjectile
    {
        private const string AimTexturePath = "MichaelWeaponsMod/Assets/AimTexture"; // The folder path to the flail chain sprite

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
            Projectile.light = 0f; // How much light emit around the projectile
            Projectile.timeLeft = 600; // The live time for the projectile (60 = 1 second, so 600 is 10 seconds)
            Projectile.maxPenetrate = -1;
            Projectile.penetrate = -1;
        }
        public AiState PrevState = AiState.Wandering;
        // Track the angle around the player (store this as a class variable)
        float orbitAngle = 0f;
        public override void AI()
        {
            CurrentAIState = AiState.Wandering;
            float maxDistance = 120;
            Projectile.rotation = Projectile.velocity.ToRotation();
            // First, we find a homing target if we don't have one
            //if (HomingTarget == null) { 
            HomingTarget = FindClosestNPC(maxDistance);
            //}

            // If we have a homing target, make sure it is still valid. If the NPC dies or moves away, we'll want to find a new target
            if (HomingTarget != null && !IsValidTarget(HomingTarget))
            {
                HomingTarget = null;
            }

            if (HomingTarget == null)
            {
                if (PrevState != AiState.Wandering) { Projectile.Kill(); }
            }
            else
            {
                CurrentAIState = AiState.Chasing;
                var target = HomingTarget.Center;
                // If found, we rotate the projectile velocity in the direction of the target.
                // We only rotate by 3 degrees an update to give it a smooth trajectory. Increase the rotation speed here to make tighter turns

                float length = Projectile.velocity.Length();
                float targetAngle = Projectile.AngleTo(target);
                Projectile.velocity = Projectile.velocity.ToRotation().AngleTowards(targetAngle, MathHelper.ToRadians(15)).ToRotationVector2() * length;
                Projectile.rotation = Projectile.velocity.ToRotation();
                Projectile.ai[2] = Projectile.ai[2] < 110 ? Math.Max(Projectile.ai[2]++, maxDistance - Projectile.Center.Distance(target)) : Projectile.ai[2] + 3;
                var size = Math.Max(HomingTarget.height, HomingTarget.width);
                if ((Projectile.ai[2] >= 120f + (size * 1.4f)))
                {
                    Projectile.ai[2] = 120f + (size * 1.4f);
                }
                PrevState = AiState.Chasing;
            }
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
                        bool IsTrackable = false;
                        foreach (var npc in NPCTracker.npcDictionary)
                        {
                            if (npc.Key == target && npc.Value < Projectile.timeLeft)
                            {
                                IsTrackable = true;
                                NPCTracker.npcDictionary[npc.Key] = Projectile.timeLeft;
                                break;
                            }
                        }
                        if (IsTrackable)
                        {
                            sqrMaxDetectDistance = sqrDistanceToTarget;
                            closestNPC = target;
                        }
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
            var Center = Projectile.Center; // your center position here;
            float Radius = Math.Max(120f - Projectile.ai[2], 0); // The radius of the circle
            int Segments = (int)(Radius / 6); // The number of segments (points) in the circle
            Vector2 origin = new Vector2(textureToDraw.Width() / 2f, textureToDraw.Height() / 2f);// The origin of the texture (center point of the texture)
            rot += +0.044f;
            // Loop through each segment to draw the texture at each point around the circle
            if (Radius > 0)
            {
                for (int i = 0; i < Segments; ++i)
                {
                    // Calculate the current rotation in radians
                    float currentRotation = (MathHelper.TwoPi / Segments) * i + rot;

                    // Get the offset from the center using the rotation and radius
                    Vector2 offset = currentRotation.ToRotationVector2() * Radius;

                    // Calculate the draw position by adding the offset to the center
                    Vector2 drawPosition = Center + offset;

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
            }
            return true;
        }
    }
}