using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Subdivision {

	public Mesh mesh;
	public Vector2[] points;
	float streetwidth;

	public Subdivision(Vector2 topleft, Vector2 topright, Vector2 botleft, Vector2 botright, float sw) {

		mesh = new Mesh ();
		points = new Vector2[4];
		streetwidth = sw;

		points [0] = new Vector2(topleft.x, topleft.y);
		points [1] = new Vector2(topright.x, topright.y);
		points [2] = new Vector2(botleft.x, botleft.y);
		points [3] = new Vector2 (botright.x, botright.y);

		createMesh ();

	}

	void createMesh() {
		Triangulator tr = new Triangulator (points);
		int[] indices = tr.Triangulate ();

		Vector3[] vertices = new Vector3[points.Length];
		for (int i = 0; i < vertices.Length; i++) {
			vertices [i] = new Vector3(points [i].x, 0, points [i].y);
		}
		mesh.vertices = vertices;
		mesh.triangles = indices;
		mesh.RecalculateNormals ();
		mesh.RecalculateBounds ();
	}
}
