using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YarnScapes.Core;

namespace YarnScapes.Presentation
{
    // Real tube meshes and separate flyaway fibers, rendered into a transparent
    // orthographic layer. The UI grid remains the source of gameplay input.
    public sealed class PlushYarnBoard : MonoBehaviour
    {
        private const int RenderLayer = 30;
        private const int CellsPerChunk = 24;
        private const int Steps = 12;
        private const int Sides = 16;
        private LevelDefinition level;
        private GameObject stage;
        private Camera yarnCamera;
        private RawImage display;
        private Material strandMaterial, fiberMaterial, pileMaterial, compositeMaterial;
        private RenderTexture target;
        private readonly List<Chunk> chunks = new List<Chunk>();
        private readonly HashSet<int> filled = new HashSet<int>();
        private bool dirty;
        private float animateUntil;
        private int cellCount;
        private readonly Vector3[] corners = new Vector3[4];
        public int CellCount => filled.Count;
        public int VertexCount { get; private set; }
        public bool Ready => yarnCamera != null;
        public bool HasSoftFiberCoat => pileMaterial != null;

        public bool Initialize(LevelDefinition definition)
        {
            level = definition;
            var bodyShader = Resources.Load<Shader>("YarnMaterialsV3/SoftWoolStrand");
            var hairShader = Resources.Load<Shader>("YarnMaterialsV3/SoftWoolFiber");
            var pileShader = Resources.Load<Shader>("YarnMaterialsV3/SoftWoolPile");
            var uiShader = Resources.Load<Shader>("YarnMaterialsV2/PlushComposite");
            if (bodyShader == null || hairShader == null || pileShader == null || uiShader == null || !bodyShader.isSupported || !hairShader.isSupported || !pileShader.isSupported || !uiShader.isSupported)
            { Debug.LogError("Plush yarn shaders are unavailable."); return false; }
            strandMaterial = new Material(bodyShader);
            var fiberTexture = Resources.Load<Texture2D>("YarnMaterialsV3/wool_filaments");
            if(fiberTexture == null) { Debug.LogError("Soft wool filament data are unavailable."); return false; }
            strandMaterial.SetTexture("_FiberTex", fiberTexture);
            fiberMaterial = new Material(hairShader);
            pileMaterial = new Material(pileShader);
            pileMaterial.SetTexture("_FiberTex", fiberTexture);
            compositeMaterial = new Material(uiShader);
            stage = new GameObject("PlushYarn3DStage");
            stage.transform.position = new Vector3(2000, 0, 0);
            var cameraObject = new GameObject("YarnCamera", typeof(Camera));
            cameraObject.transform.SetParent(stage.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 0, -6);
            yarnCamera = cameraObject.GetComponent<Camera>();
            yarnCamera.enabled = false;
            yarnCamera.orthographic = true;
            yarnCamera.orthographicSize = level.height * .5f;
            yarnCamera.nearClipPlane = .1f; yarnCamera.farClipPlane = 20;
            yarnCamera.clearFlags = CameraClearFlags.SolidColor;
            yarnCamera.backgroundColor = Color.clear;
            yarnCamera.cullingMask = 1 << RenderLayer;
            yarnCamera.allowHDR = false;
            yarnCamera.allowMSAA = true;
            var surface = new GameObject("RaisedWoolSurface", typeof(RectTransform), typeof(RawImage));
            surface.transform.SetParent(transform, false);
            display = surface.GetComponent<RawImage>();
            display.raycastTarget = false;
            display.material = compositeMaterial;
            var rect = display.rectTransform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            AllocateTarget(1024);
            return true;
        }

        public void Fill(int index, Color sourceColor, bool animate = true)
        {
            if (!filled.Add(index)) return;
            if (cellCount % CellsPerChunk == 0) chunks.Add(new Chunk(stage.transform, strandMaterial, fiberMaterial, pileMaterial, RenderLayer));
            var chunk = chunks[chunks.Count-1];
            var previousVertices = chunk.body.positions.Count + chunk.hair.positions.Count;
            var baseColor = new Color(.065f + sourceColor.r*.90f, .065f + sourceColor.g*.90f, .065f + sourceColor.b*.90f, 1);
            var center = new Vector2(index % level.width - level.width*.5f + .5f, level.height*.5f - index / level.width - .5f);
            var birth = animate ? Time.time : Time.time-1;
            var meta = new Vector4(center.x, center.y, birth, 0);
            var rng = new System.Random(index*7919+326863);
            Backing(chunk.body, center, baseColor, meta);
            for (var row=0; row<2; row++)
            for (var col=0; col<2; col++)
            {
                var origin = center + new Vector2(-.5f+col*.5f, -.5f+row*.5f);
                var radius = .097f * (.94f + Next(rng)*.12f);
                Strand(chunk, origin, false, radius, baseColor, meta, rng);
                Strand(chunk, origin, true, radius, baseColor, meta, rng);
            }
            chunk.needsUpload=true;
            cellCount++;
            VertexCount += chunk.body.positions.Count + chunk.hair.positions.Count - previousVertices;
            dirty = true;
            if (animate) animateUntil = Time.unscaledTime + .23f;
        }

        private static float Next(System.Random rng) => (float)rng.NextDouble();
        private static Vector3 Curve(Vector2 origin, bool mirror, float t)
        {
            var u = 1-t;
            var x = u*u*u*.18f + 3*u*u*t*.055f + 3*u*t*t*.31f + t*t*t*.52f;
            var y = u*u*u*.91f + 3*u*u*t*.70f + 3*u*t*t*.32f + t*t*t*.12f;
            if (mirror) x=1-x;
            // Bury the strand ends in the knit bed; a cut-off tube end reads
            // as molded plastic, especially in the oblique acceptance view.
            var height=.058f + Mathf.Sin(t*Mathf.PI)*.063f;
            // One leg rises over the other near the foot of the stitch.
            height += (mirror ? -.014f : .016f)*Mathf.Exp(-Mathf.Pow((t-.86f)*5,2));
            return new Vector3(origin.x+x*.5f, origin.y+y*.5f, -height);
        }
        private static void Strand(Chunk chunk, Vector2 origin, bool mirror, float radius, Color color, Vector4 meta, System.Random rng)
        {
            var start=chunk.body.positions.Count;
            var phase=Next(rng)*Mathf.PI*2;
            for (var s=0; s<=Steps; s++)
            {
                var t=s/(float)Steps;
                var c=Curve(origin,mirror,t);
                var tangent=(Curve(origin,mirror,Mathf.Min(1,t+.005f))-Curve(origin,mirror,Mathf.Max(0,t-.005f))).normalized;
                var axis=new Vector3(tangent.y,-tangent.x,0).normalized;
                var binormal=Vector3.Cross(tangent,axis).normalized;
                var thickness=radius*(.86f+.14f*Mathf.Sin(t*Mathf.PI));
                for (var j=0; j<=Sides; j++)
                {
                    var angle=j/(float)Sides*Mathf.PI*2;
                    var normal=axis*Mathf.Cos(angle)+binormal*Mathf.Sin(angle);
                    var ply=1+.012f*Mathf.Cos(t*22+angle*3+phase)+.008f*Mathf.Sin(t*39+angle*2+phase);
                    var point=c+normal*thickness*ply;
                    var tint=color;
                    tint.a=.73f+.27f*Mathf.Sin(t*Mathf.PI);
                    chunk.body.Add(point,normal,tint,new Vector2(t*.8f+phase*.37f,angle/(Mathf.PI*2)+phase*.19f),meta);
                    if (s==Steps || j==Sides) continue;
                    var a=start+s*(Sides+1)+j;
                    chunk.body.Triangle(a,a+1,a+Sides+1);
                    chunk.body.Triangle(a+1,a+Sides+2,a+Sides+1);
                }
            }
            // Attach flyaways to the actual tube frame. A screen-space radial
            // offset can bury hairs inside the curve, making them invisible.
            for (var h=0; h<26; h++)
            {
                var t=.08f+Next(rng)*.84f;
                var c=Curve(origin,mirror,t);
                var tangent=(Curve(origin,mirror,Mathf.Min(1,t+.005f))-Curve(origin,mirror,Mathf.Max(0,t-.005f))).normalized;
                var axis=new Vector3(tangent.y,-tangent.x,0).normalized;
                var binormal=Vector3.Cross(tangent,axis).normalized;
                var angle=Next(rng)*Mathf.PI*2;
                var n=axis*Mathf.Cos(angle)+binormal*Mathf.Sin(angle);
                if(n.z>0) n.z=-n.z;
                var thickness=radius*(.86f+.14f*Mathf.Sin(t*Mathf.PI));
                var root=c+n*thickness*1.025f;
                var direction=(tangent*(Next(rng)>.5f?1:-1)*.74f+n*.36f+axis*(Next(rng)-.5f)*.3f).normalized;
                var length=h<20 ? .018f+Mathf.Pow(Next(rng),1.6f)*.047f : .052f+Next(rng)*.068f;
                var end=root+direction*length;
                var middle=root+direction*length*.5f+new Vector3(-direction.y,direction.x,0)*length*.28f;
                var tint=Color.Lerp(color,new Color(.93f,.9f,.84f),.035f)*( .88f+Next(rng)*.10f);
                tint.a=.34f+Next(rng)*.24f;
                Hair(chunk.hair,root,middle,end,.0022f+Next(rng)*.0022f,tint,meta);
            }
        }
        private static void Hair(Geometry mesh, Vector3 a, Vector3 control, Vector3 b, float width, Color color, Vector4 meta)
        {
            var start=mesh.positions.Count;
            for (var s=0;s<=3;s++)
            {
                var t=s/3f;
                var p=Bezier(a,control,b,t);
                var d=2*(1-t)*(control-a)+2*t*(b-control);
                var n=new Vector3(-d.y,d.x,0).normalized*width*Mathf.Lerp(1,.18f,t*t);
                mesh.Add(p-n,Vector3.back,color,new Vector2(t,0),meta);
                mesh.Add(p+n,Vector3.back,color,new Vector2(t,1),meta);
                if(s==3) continue;
                var v=start+s*2;
                mesh.Triangle(v,v+1,v+3); mesh.Triangle(v,v+3,v+2);
            }
        }
        private static Vector3 Bezier(Vector3 a,Vector3 control,Vector3 b,float t)
        { var u=1-t;return u*u*a+2*u*t*control+t*t*b; }
        private static void Backing(Geometry mesh,Vector2 center,Color color,Vector4 meta)
        {
            var v=mesh.positions.Count;
            color*=.83f; color.a=.5f;
            mesh.Add(new Vector3(center.x-.5005f,center.y-.5005f,.025f),Vector3.back,color,Vector2.zero,meta);
            mesh.Add(new Vector3(center.x-.5005f,center.y+.5005f,.025f),Vector3.back,color,Vector2.up,meta);
            mesh.Add(new Vector3(center.x+.5005f,center.y+.5005f,.025f),Vector3.back,color,Vector2.one,meta);
            mesh.Add(new Vector3(center.x+.5005f,center.y-.5005f,.025f),Vector3.back,color,Vector2.right,meta);
            mesh.Triangle(v,v+1,v+2);mesh.Triangle(v,v+2,v+3);
        }
        private void LateUpdate()
        {
            if (!Ready) return;
            foreach (var chunk in chunks) if (chunk.needsUpload) { chunk.Upload(); chunk.needsUpload=false; }
            display.rectTransform.GetWorldCorners(corners);
            var desired=Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.CeilToInt(Vector3.Distance(corners[0],corners[3])*1.15f)),1024,4096);
            if (target==null || target.width!=desired) AllocateTarget(desired);
            if (dirty || Time.unscaledTime<animateUntil)
            { yarnCamera.Render(); dirty=false; }
        }
        private void AllocateTarget(int width)
        {
            if(target!=null) { target.Release();Destroy(target); }
            target=new RenderTexture(width,Mathf.RoundToInt(width*level.height/(float)level.width),24,RenderTextureFormat.ARGB32);
            target.name="PlushYarnRender";target.antiAliasing=2;
            target.filterMode=FilterMode.Bilinear;target.wrapMode=TextureWrapMode.Clamp;
            target.Create();yarnCamera.targetTexture=target;display.texture=target;
            yarnCamera.aspect=level.width/(float)level.height;dirty=true;
        }
        public void Clear()
        {
            foreach(var chunk in chunks) chunk.Dispose();
            chunks.Clear();filled.Clear();cellCount=VertexCount=0;dirty=true;
        }
        public void SaveGeometryProof(string path, Vector2 focus = default(Vector2), float magnification = 3.3f)
        {
            var rotation=yarnCamera.transform.localRotation;
            var position=yarnCamera.transform.localPosition;
            var size=yarnCamera.orthographicSize;
            yarnCamera.orthographicSize=magnification;
            yarnCamera.transform.localPosition=new Vector3(focus.x,focus.y+3.4f,-6);
            yarnCamera.transform.LookAt(stage.transform.position+new Vector3(focus.x,focus.y,0));
            yarnCamera.farClipPlane=level.height*2;
            yarnCamera.Render();
            SaveTarget(path);
            yarnCamera.transform.localPosition=position;yarnCamera.transform.localRotation=rotation;
            yarnCamera.orthographicSize=size;
            yarnCamera.farClipPlane=20;yarnCamera.Render();
        }
        private void SaveTarget(string path)
        {
            var old=RenderTexture.active;RenderTexture.active=target;
            var copy=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
            copy.ReadPixels(new Rect(0,0,target.width,target.height),0,0);copy.Apply();
            System.IO.File.WriteAllBytes(path,copy.EncodeToPNG());Destroy(copy);RenderTexture.active=old;
        }
        private void OnDestroy()
        {
            Clear();if(stage!=null)Destroy(stage);
            if(target!=null){target.Release();Destroy(target);}
            if(strandMaterial!=null)Destroy(strandMaterial);if(fiberMaterial!=null)Destroy(fiberMaterial);
            if(pileMaterial!=null)Destroy(pileMaterial);
            if(compositeMaterial!=null)Destroy(compositeMaterial);
        }
        private sealed class Chunk
        {
            public readonly Geometry body,hair;
            private readonly GameObject root;
            public bool needsUpload;
            public Chunk(Transform parent,Material bodyMaterial,Material hairMaterial,Material coatMaterial,int layer)
            {
                root=new GameObject("YarnMeshChunk");root.transform.SetParent(parent,false);
                body=new Geometry(root.transform,"PlumpLoops",bodyMaterial,layer);
                hair=new Geometry(root.transform,"FlyawayFibers",hairMaterial,layer);
                body.AddCoat(root.transform,coatMaterial,layer);
            }
            public void Upload()
            {
                body.Upload();hair.Upload();
            }
            public void Dispose(){if(root!=null)root.SetActive(false);body.Dispose();hair.Dispose();Destroy(root);}
        }
        private sealed class Geometry
        {
            public readonly List<Vector3> positions=new List<Vector3>();
            private readonly List<Vector3> normals=new List<Vector3>();
            private readonly List<Color> colors=new List<Color>();
            private readonly List<Vector2> uv=new List<Vector2>();
            private readonly List<Vector4> cells=new List<Vector4>();
            private readonly List<int> triangles=new List<int>();
            private readonly Mesh mesh;
            public Geometry(Transform parent,string name,Material material,int layer)
            {
                var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
                go.transform.SetParent(parent,false);go.layer=layer;
                mesh=new Mesh{name=name};mesh.MarkDynamic();
                go.GetComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows=false;
            }
            public void Add(Vector3 p,Vector3 n,Color c,Vector2 tex,Vector4 cell)
            {positions.Add(p);normals.Add(n);colors.Add(c);uv.Add(tex);cells.Add(cell);}
            public void AddCoat(Transform parent, Material material, int layer)
            {
                var go=new GameObject("FineSurfacePile",typeof(MeshFilter),typeof(MeshRenderer));
                go.transform.SetParent(parent,false);go.layer=layer;
                go.GetComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            }
            public void Triangle(int a,int b,int c){triangles.Add(a);triangles.Add(b);triangles.Add(c);}
            public void Upload()
            {mesh.SetVertices(positions);mesh.SetNormals(normals);mesh.SetColors(colors);mesh.SetUVs(0,uv);mesh.SetUVs(1,cells);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();}
            public void Dispose(){Destroy(mesh);}
        }
    }
}
