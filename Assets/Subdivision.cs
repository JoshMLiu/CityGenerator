using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Subdivision {

	public Mesh mesh;
	public Vector3[] points;
	float streetwidth;

	public Subdivision(Vector2 topleft, Vector2 topright, Vector2 botleft, Vector2 botright, float sw) {

		points = new Vector3[8];
		streetwidth = sw;

		if (topright.x - topleft.x <= 3*streetwidth) {
			return;
		}
		if (botright.x - botleft.x <= 3*streetwidth) {
			return;
		}
		if (topleft.y - botleft.y <= 3*streetwidth) {
			return;
		}
		if (topright.y - botright.y <= 3*streetwidth) {
			return;
		}

		points [0] = new Vector3(topleft.x + streetwidth, 0, topleft.y - streetwidth);
		points [1] = new Vector3(topright.x - streetwidth, 0, topright.y - streetwidth);
		points [2] = new Vector3(botleft.x + streetwidth, 0, botleft.y + streetwidth);
		points [3] = new Vector3 (botright.x - streetwidth, 0, botright.y + streetwidth);

		float rand = Random.Range (0f, 1f);

		points [4] = new Vector3(topleft.x, rand, topleft.y);
		points [5] = new Vector3(topright.x, rand, topright.y);
		points [6] = new Vector3(botleft.x, rand, botleft.y);
		points [7] = new Vector3 (botright.x, rand, botright.y);

		mesh = new Mesh ();
		createMesh ();

	}

	void createMesh() {

		int[] indices = new int[36];

		indices [0] = 0;
		indices [1] = 1;
		indices [2] = 2;
		indices [3] = 2;
		indices [4] = 1;
		indices [5] = 3;

		/**
		indices [6] = 4;
		indices [7] = 5;
		indices [8] = 6;
		indices [9] = 5;
		indices [10] = 6;
		indices [11] = 7;
		**/
 
		mesh.vertices = points;
		mesh.triangles = indices;
		mesh.RecalculateNormals ();
		mesh.RecalculateBounds ();
	}
}
