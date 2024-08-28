using MichaelWeaponsMod.Content.Projectiles;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using ReLogic.Content;
using System.Collections.Generic;
using System;
using Terraria.ModLoader;
using Terraria;

namespace MichaelWeaponsMod.Content.NPCs.NPCDictionaryStuff
{
    public static class NPCTracker
    {
        public static Dictionary<NPC, int> npcDictionary = new Dictionary<NPC, int>();
    }
    public class DictUpdater : GlobalNPC
    {
        private const string AimTexturePath = "MichaelWeaponsMod/Assets/AimTexture";

        private static Asset<Texture2D> aimTexture;
        public override void Load()
        {
            aimTexture = ModContent.Request<Texture2D>(AimTexturePath);
        }
        public override bool InstancePerEntity => true;
        public override void AI(NPC npc)
        {
            NPCTracker.npcDictionary.TryAdd(npc, 0);
        }
        public override void ResetEffects(NPC npc)
        {
            if (!npc.active || npc == null) { NPCTracker.npcDictionary.Remove(npc); }
        }
        public override void PostAI(NPC npc)
        {
            foreach (var entry in NPCTracker.npcDictionary)
            {
                if (entry.Value != 0) { NPCTracker.npcDictionary[entry.Key]--; }
            }

        }
        public float pulse = 1f;
        public bool IsPulsing = false;
        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.type != ModContent.ProjectileType<MichaelArrow>()) { return; }
            pulse = 0.06f;
            IsPulsing = true;
        }
        public float rot = 0f;
        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!NPCTracker.npcDictionary.ContainsKey(npc)) { return; }
            if (NPCTracker.npcDictionary[npc] == 0) { return; }
            var textureToDraw = aimTexture;// your texture here;
            var radius = Math.Max(npc.height, npc.width);
            var Radius = radius * 1.4f / (1f + pulse);
            var segments = 5;
            var Segments = 10;
            var center = npc.Center;
            Vector2 origin = new Vector2(textureToDraw.Width() / 2f, textureToDraw.Height() / 2f);// The origin of the texture (center point of the texture)
            rot += +0.033f;
            pulse = IsPulsing ? pulse + 0.06f : pulse - 0.06f;
            pulse = Math.Clamp(pulse, 0f, 0.42f);
            if (IsPulsing && pulse == 0.42f) { IsPulsing = false; }

            // Loop through each segment to draw the texture at each point around the circle
            if (radius > 0)
            {
                for (int i = 0; i < segments; ++i)
                {
                    // Calculate the current rotation in radians
                    float currentRotation = (MathHelper.TwoPi / segments) * i + rot;

                    // Get the offset from the center using the rotation and radius
                    Vector2 offset = currentRotation.ToRotationVector2() * radius;

                    // Calculate the draw position by adding the offset to the center
                    Vector2 drawPosition = center + offset;

                    // Draw the texture at the calculated position
                    DrawingAim(textureToDraw.Value, drawPosition, currentRotation + MathHelper.PiOver2, origin);
                    EmitLight(center, radius);
                }
                for (int i = 0; i < Segments; ++i)
                {
                    // Calculate the current rotation in radians
                    float Rotation = (MathHelper.TwoPi / Segments) * i - rot;
                    // Get the offset from the center using the rotation and radius
                    Vector2 Offset = Rotation.ToRotationVector2() * Radius;

                    // Calculate the draw position by adding the offset to the center
                    Vector2 drawPosition = center + Offset;

                    // Draw the texture at the calculated position
                    DrawingAim(textureToDraw.Value, drawPosition, Rotation + MathHelper.PiOver2, origin);
                }
            }
        }
        public void DrawingAim(Texture2D texture, Vector2 position, float rotation, Vector2 origin)
        {
            Main.spriteBatch.Draw(
                texture,
                position - Main.screenPosition,
                null,
                Color.White,
                rotation,
                origin,
                1f,
                SpriteEffects.None,
                1f
            );
        }
        public void EmitLight(Vector2 position, float power)
        {
            float r = 0.0022f * power;
            float g = 0.0041f * power;
            float b = 0.0067f * power;
            Lighting.AddLight(position, r, g, b);
        }
    }
}