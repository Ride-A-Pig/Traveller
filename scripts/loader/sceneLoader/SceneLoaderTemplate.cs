using System.Threading.Tasks;
using Godot;

namespace ColdMint.scripts.loader.sceneLoader;

/// <summary>
/// <para>场景加载器模板</para>
/// <para>场景加载器模板</para>
/// </summary>
public partial class SceneLoaderTemplate : Node2D, ISceneLoaderContract
{
    public sealed override async void _Ready()
    {
        await InitializeData();
        await LoadScene();
    }


    public virtual Task InitializeData()
    {
        return Task.CompletedTask;
    }

    public virtual Task LoadScene()
    {
        return Task.CompletedTask;
    }
}