namespace SamplePlugin.Core.Application;

public interface IMainWindow
{
    bool IsOpen { get; set; }
    void Draw();
}