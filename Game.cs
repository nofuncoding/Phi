using Godot;
using Phi.Chart.Original;
using System;

namespace Phi;

public partial class Game : Node
{
	[Export]
	string _chartPath;

	[Export]
	string _musicPath;

	[Export]
	string _imgPath;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		OriginalPlayer player = new(_chartPath, _musicPath);
		AddChild(player);
		player.Play();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
