using Fall.Shared;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Fall.Engine
{
  public class mesh
  {
    private readonly draw_mode _drawMode;
    private readonly ibo _ibo;
    private readonly shader _shader;
    private readonly bool _static;
    private readonly vao _vao;
    private readonly vbo _vbo;
    private bool _building;
    private int _index;
    private int _tris;
    private int _vertex;

    public mesh(draw_mode drawMode, shader shader, bool @static, params vao.attrib[] attribs)
    {
      _drawMode = drawMode;
      _shader = shader;
      int stride = attribs.Sum(attrib => (int)attrib * sizeof(float));
      _vbo = new vbo(stride * drawMode.Size * sizeof(float), @static);
      _vbo.Bind();
      _ibo = new ibo(drawMode.Size * 128 * sizeof(float), @static);
      _ibo.Bind();
      _vao = new vao(attribs);
      vbo.Unbind();
      ibo.Unbind();
      vao.Unbind();
      _static = @static;
    }

    public int Next()
    {
      return _vertex++;
    }

    public mesh Float1(float p0)
    {
      _vbo.Put(p0);
      return this;
    }

    public mesh Float2(float p0, float p1)
    {
      _vbo.Put(p0);
      _vbo.Put(p1);
      return this;
    }

    public mesh Float2(Vector2 p0)
    {
      _vbo.Put(p0.X);
      _vbo.Put(p0.Y);
      return this;
    }

    public mesh Float3(float p0, float p1, float p2)
    {
      _vbo.Put(p0);
      _vbo.Put(p1);
      _vbo.Put(p2);
      return this;
    }

    public mesh Float4(float p0, float p1, float p2, float p3)
    {
      _vbo.Put(p0);
      _vbo.Put(p1);
      _vbo.Put(p2);
      _vbo.Put(p3);
      return this;
    }

    public mesh Float4(uint color)
    {
      return Float4(((color >> 16) & 0xff) * 0.003921569f, ((color >> 8) & 0xff) * 0.003921569f,
        (color & 0xff) * 0.003921569f, ((color >> 24) & 0xff) * 0.003921569f);
    }

    public mesh Float4(uint color, float alpha)
    {
      return Float4(((color >> 16) & 0xff) * 0.003921569f, ((color >> 8) & 0xff) * 0.003921569f,
        (color & 0xff) * 0.003921569f, alpha);
    }

    public void Single(int p0)
    {
      _ibo.Put(p0);
      _index++;
    }

    public void Line(int p0, int p1)
    {
      _ibo.Put(p0);
      _ibo.Put(p1);
      _index += 2;
    }

    public void Tri(int p0, int p1, int p2)
    {
      _ibo.Put(p0);
      _ibo.Put(p1);
      _ibo.Put(p2);
      _index += 3;
      _tris++;
    }

    public void Quad(int p0, int p1, int p2, int p3)
    {
      _ibo.Put(p0);
      _ibo.Put(p1);
      _ibo.Put(p2);
      _ibo.Put(p2);
      _ibo.Put(p3);
      _ibo.Put(p0);
      _index += 6;
      _tris += 2;
    }

    public void Begin()
    {
      if (_building) throw new Exception("Already building");
      if (!_static)
      {
        _vbo.Clear();
        _ibo.Clear();
        _tris = 0;
      }

      _vertex = 0;
      _index = 0;
      _building = true;
    }

    public void End()
    {
      if (!_building) throw new Exception("Not building");

      if (_index > 0)
      {
        _vbo.Upload();
        _ibo.Upload();
      }

      if (_static)
      {
        _vbo.Clear();
        _ibo.Clear();
      }

      _building = false;
    }

    public void Render()
    {
      if (_building) End();

      if (_index <= 0) return;
      gl_state_manager.SaveState();
      gl_state_manager.EnableBlend();
      if (glh.Rendering3d)
        gl_state_manager.EnableDepth();
      else
        gl_state_manager.DisableDepth();
      _shader?.Bind();
      _shader?.SetDefaults();
      _vao.Bind();
      _ibo.Bind();
      _vbo.Bind();
      GL.DrawElements(_drawMode.AsGl(), _index, DrawElementsType.UnsignedInt, 0);
      ibo.Unbind();
      vbo.Unbind();
      vao.Unbind();
      gl_state_manager.RestoreState();
      fall.Tris += _tris;
    }

    public void RenderInstanced(int numInstances)
    {
      if (_building) End();

      if (_index <= 0 || numInstances <= 0) return;
      gl_state_manager.SaveState();
      gl_state_manager.EnableBlend();
      if (glh.Rendering3d)
        gl_state_manager.EnableDepth();
      else
        gl_state_manager.DisableDepth();
      _shader?.Bind();
      _shader?.SetDefaults();
      _vao.Bind();
      _ibo.Bind();
      _vbo.Bind();
      GL.DrawElementsInstanced(_drawMode.AsGlPrim(), _index, DrawElementsType.UnsignedInt, IntPtr.Zero, numInstances);
      ibo.Unbind();
      vbo.Unbind();
      vao.Unbind();
      gl_state_manager.RestoreState();
      fall.Tris += _tris * numInstances;
    }

    public sealed class draw_mode
    {
      public static readonly draw_mode LINE = new(2, BeginMode.Lines, PrimitiveType.Lines);
      public static readonly draw_mode TRIANGLE = new(3, BeginMode.Triangles, PrimitiveType.Triangles);
      public static readonly draw_mode TRIANGLE_STRIP = new(2, BeginMode.TriangleStrip, PrimitiveType.TriangleStrip);
      private readonly BeginMode _mode;
      private readonly PrimitiveType _prim;
      public readonly int Size;

      private draw_mode(int size, BeginMode mode, PrimitiveType prim)
      {
        Size = size;
        _mode = mode;
        _prim = prim;
      }

      public override bool Equals(object obj)
      {
        if (obj is draw_mode mode) return _mode == mode._mode;

        return false;
      }

      public override int GetHashCode()
      {
        return _mode.GetHashCode();
      }

      public BeginMode AsGl()
      {
        return _mode;
      }

      public PrimitiveType AsGlPrim()
      {
        return _prim;
      }
    }
  }
}