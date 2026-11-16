using UnityEngine;
using UnityEngine.UI;

namespace YarnScapes.Presentation
{
    internal static class FrameworkUI
    {
        public static readonly Color Ink = new Color(.16f,.19f,.24f);
        public static readonly Color Muted = new Color(.46f,.49f,.54f);
        public static readonly Color Accent = new Color(.96f,.42f,.52f);
        public static readonly Color Paper = new Color(.985f,.975f,.96f);
        private static Font font;
        public static Font Font
        {
            get
            {
                if (font == null)
                {
                    font = Resources.Load<Font>("Fonts/Rubik-Regular");
                    if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
                        font = UnityEngine.Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 32);
                }
                return font;
            }
        }
        public static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = new Vector2(x,y); rect.anchorMax = new Vector2(x+w,y+h);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        public static Image Panel(Transform parent, string name, Color color, float x=0,float y=0,float w=1,float h=1)
        {
            var go = new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false);
            var image = go.GetComponent<Image>(); image.color=color; Place(image.rectTransform,x,y,w,h); return image;
        }
        public static Text Label(Transform parent,string value,int size,float x,float y,float w,float h, Color? color=null, TextAnchor align=TextAnchor.MiddleLeft)
        {
            var go=new GameObject("Text",typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false);
            var text=go.GetComponent<Text>(); text.font=Font; text.text=value; text.fontSize=size; text.color=color??Ink;
            text.alignment=align; text.raycastTarget=false; text.horizontalOverflow=HorizontalWrapMode.Wrap;
            text.verticalOverflow=VerticalWrapMode.Truncate; Place(text.rectTransform,x,y,w,h); return text;
        }
        public static Button Button(Transform parent,string name,string caption,float x,float y,float w,float h,System.Action action, bool primary=false)
        {
            var panel=Panel(parent,name,primary?Accent:Color.white,x,y,w,h);
            var button=panel.gameObject.AddComponent<Button>(); button.targetGraphic=panel;
            var colors=button.colors; colors.pressedColor=new Color(.85f,.85f,.85f); button.colors=colors;
            if (!string.IsNullOrEmpty(caption)) Label(panel.transform,caption,29,0,0,1,1,primary?Color.white:Ink,TextAnchor.MiddleCenter);
            if(action!=null) button.onClick.AddListener(()=>action()); return button;
        }
        public static RectTransform Scroll(Transform parent,string name,float x,float y,float w,float h,bool horizontal=false)
        {
            var view=Panel(parent,name,Color.clear,x,y,w,h); view.gameObject.AddComponent<RectMask2D>();
            var obj=new GameObject(name+"Content",typeof(RectTransform)); obj.transform.SetParent(view.transform,false);
            var rect=obj.GetComponent<RectTransform>();
            rect.anchorMin=horizontal?Vector2.zero:new Vector2(0,1); rect.anchorMax=horizontal?new Vector2(0,1):Vector2.one;
            rect.pivot=new Vector2(0,1); rect.sizeDelta=Vector2.zero;
            var scroll=view.gameObject.AddComponent<ScrollRect>(); scroll.viewport=view.rectTransform; scroll.content=rect;
            scroll.horizontal=horizontal; scroll.vertical=!horizontal; scroll.movementType=ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity=45; return rect;
        }
    }
}
