using Terraria.ModLoader;
using Terraria.ID;
using Terraria;
using Microsoft.Xna.Framework;
using MichaelWeaponsMod.Content.Projectiles;
using MichaelWeaponsMod.Content.MathStuff;
using System;
using Terraria.DataStructures;

namespace MichaelWeaponsMod.Content.Items
{
    public class MichaelsBow : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 60;
            Item.DamageType = DamageClass.Ranged;
            Item.knockBack = 3;
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAmmo = AmmoID.Arrow;
            Item.shootSpeed = 1;
            Item.width = 20;
            Item.height = 50;
            Item.noMelee = true;
        }
        int timer = 400;
        public override void HoldItem(Player player)
        {
            if (timer == 400)
            {
                var request = new AdvancedPopupRequest();
                request.Color = Color.White;
                request.DurationInFrames = 200;
                request.Text = "Michael...";
                request.Velocity = new Vector2(0, 0);
                var rand = new Random();
                var Rand = new Random();
                PopupText.NewText(request, player.MountedCenter + new Vector2(20f * ((float)(rand.NextDouble() * 2) - 1), 20f * ((float)(Rand.NextDouble() * 2) - 1)));
                timer = timer >= 0 ? timer-- : 400;
            }
        }
        public bool CanBeUsed = false;
        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (CanBeUsed)
            {
                var mouse = new Vector2(Main.screenPosition.X + Main.mouseX, Main.screenPosition.Y + Main.mouseY);
                player.direction = Main.mouseX > Main.screenWidth / 2 ? 1 : -1;
                player.itemRotation = player.Center.DirectionTo(mouse).ToRotation();
                player.itemRotation = player.direction == 1 ? player.itemRotation : player.itemRotation + MathHelper.Pi;
                CanBeUsed = false;
            }
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            CanBeUsed = true;
            position = CalculateSpawn();
            velocity = position.DirectionTo(player.Center) * 9;
            return base.Shoot(player, source, position, velocity, type, damage, knockback);
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
                case 1: x = Main.screenPosition.X + Main.screenWidth; y = point.Y; break;
                case 2: x = point.X; y = Main.screenPosition.Y + Main.screenHeight; break;
                case 3: x = Main.screenPosition.X; y = point.Y; break;
            }
            return position = new Vector2(x, y);
        }
    }
}