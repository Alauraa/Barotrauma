﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Subsurface
{
    class GUIButton : GUIComponent
    {
        protected GUITextBlock textBlock;

        public delegate bool OnClickedHandler(GUIButton button, object obj);
        public OnClickedHandler OnClicked;

        public delegate bool OnPressedHandler();
        public OnPressedHandler OnPressed;

        public bool Enabled { get; set; }
        
        public string Text
        {
            get { return textBlock.Text; }
            set { textBlock.Text = value; }
        }

        public GUIButton(Rectangle rect, string text, GUIStyle style, Alignment alignment, GUIComponent parent = null)
            : this(rect, text, style.foreGroundColor, alignment, parent)
        {
            hoverColor = style.hoverColor;
            selectedColor = style.selectedColor;
        }

        public GUIButton(Rectangle rect, string text, Color color, GUIComponent parent = null)
            : this(rect, text, color, (Alignment.Left | Alignment.Top), parent)
        {
        }
        
        public GUIButton(Rectangle rect, string text, Color color, Alignment alignment, GUIComponent parent = null)
        {
            this.rect = rect;
            this.color = color;
            this.alignment = alignment;

            Enabled = true;

            if (parent != null)
                parent.AddChild(this);
            
            textBlock = new GUITextBlock(new Rectangle(0,0,0,0), text, Color.Transparent, Color.Black, (Alignment.CenterX | Alignment.CenterY), this);

        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (rect.Contains(PlayerInput.GetMouseState.Position) && Enabled)
            {
                state = ComponentState.Hover;
                if (PlayerInput.GetMouseState.LeftButton == ButtonState.Pressed)
                {
                    if (OnPressed != null)
                    {
                        if (OnPressed()) state = ComponentState.Selected;
                    }
                }
                else if (PlayerInput.LeftButtonClicked())
                {
                    if (OnClicked != null)
                    {
                        if (OnClicked(this, UserData)) state = ComponentState.Selected;
                    }
                    
                }
            }
            else
            {
                state = ComponentState.None;
            }

            Color currColor = color;
            if (state == ComponentState.Hover) currColor = hoverColor;
            if (state == ComponentState.Selected) currColor = selectedColor;

            GUI.DrawRectangle(spriteBatch, rect, currColor * alpha, true);

            //spriteBatch.DrawString(HUD.font, text, new Vector2(rect.X+rect.Width/2, rect.Y+rect.Height/2), Color.Black, 0.0f, new Vector2(0.5f,0.5f), 1.0f, SpriteEffects.None, 0.0f);

            GUI.DrawRectangle(spriteBatch, rect, Color.Black * alpha, false);

            DrawChildren(spriteBatch);

            if (!Enabled) GUI.DrawRectangle(spriteBatch, rect, Color.Gray*0.5f*alpha, true);
        }
    }
}
