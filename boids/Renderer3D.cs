using OpenTK;

namespace boids
{
	public class Renderer3D
	{
		public void DrawModel(Model model, Shader shader, Vector3 position, Vector3 size, Vector3 color, float aspectRatio)
		{
			shader.Use();
			shader.SetMatrix4("projection", Matrix4.CreatePerspectiveFieldOfView(Utils.Deg2Rad(Engine.Camera.Zoom), aspectRatio, 0.1f, 100f));
			shader.SetMatrix4("view", Engine.Camera.GetViewMatrix());

			var modelMatrix = Matrix4.CreateScale(size);

			modelMatrix *= Matrix4.CreateTranslation(position);
			shader.SetMatrix4("model", modelMatrix);

			model.Draw(shader);
		}
	}
}
