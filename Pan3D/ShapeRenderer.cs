using Microsoft.DirectX.Direct3D;

namespace Terry
{
    public interface ShapeRenderer
    {
        RenderData OnDeviceChange(Device device);
        RenderData OnNextShape(Device device);
        RenderData OnTimerEvent(double time, Device device);
    }
}
