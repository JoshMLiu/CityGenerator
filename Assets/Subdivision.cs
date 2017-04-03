using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Subdivision {

	public Mesh mesh;
	public Vector3[] points;
	float streetwidth;
	public Statistics stats;

	public Subdivision(Vector2 topleft, Vector2 topright, Vector2 botleft, Vector2 botright, float sw) {

		points = new Vector3[4];
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

		mesh = new Mesh ();
		createMesh ();

		stats = new Statistics ();
	}

	void createMesh() {

		int[] indices = new int[6];

		indices [0] = 0;
		indices [1] = 1;
		indices [2] = 2;
		indices [3] = 2;
		indices [4] = 1;
		indices [5] = 3;
 
		mesh.vertices = points;
		mesh.triangles = indices;
		mesh.RecalculateNormals ();
		mesh.RecalculateBounds ();
	}

	public List<Mesh> getBuildingMeshes(float outputchance) {

		List<Mesh> meshes = new List<Mesh> ();

		float minx = Mathf.Min (points [0].x, points [2].x);
		float maxx = Mathf.Max (points [1].x, points [3].x);

		float miny = Mathf.Min (points [2].z, points [3].z);
		float maxy = Mathf.Max (points [0].z, points [1].z);

		float currentx = minx;
		float currenty = maxy;

		while (currenty >= miny) {
			float randy = Random.Range (0.6f, 1f);
			float nexty = currenty - randy;
			if (nexty < miny) {
				nexty = miny - 0.01f;
			}
			currentx = minx;
			while (currentx <= maxx) {
				float randx = Random.Range (0.6f, 1f);
				float nextx = currentx + randx;
				if (nextx > maxx) {
					nextx = maxx + 0.01f;
				}

				Vector2 midpoint = new Vector2 ((currentx + nextx) / 2, (currenty + nexty) / 2);

				float lengthx = nextx - currentx;
				float lengthy = currenty - nexty;

				if (containsPointInMesh (midpoint) && lengthx > randx / 2 && lengthy > randy / 2) {

					Vector2 tl = new Vector2 (currentx, currenty);
					Vector2 tr = new Vector2 (nextx, currenty);
					Vector2 bl = new Vector2 (currentx, nexty);
					Vector2 br = new Vector2 (nextx, nexty);

					Statistics.BuildingType btype = stats.getRandomBuilding ();

					if (btype == Statistics.BuildingType.TOWER) {
						float rand = Random.Range (0f, 1f);
						if (rand <= outputchance) {
							float h = (float)stats.getRandomTowerHeight ();
							meshes.Add(createBuildingMesh(tl, tr, bl, br, h/10));
						}

					} else {
						float rand = Random.Range (0f, 1f);
						if (rand <= outputchance) {
							float randheight = Random.Range (0.3f, 0.9f);
							meshes.Add (createBuildingMesh (tl, tr, bl, br, randheight));
						}
					}		
				}
				currentx = nextx;
			}
			currenty = nexty;
		}

		return meshes;
	}

	public Mesh createBuildingMesh(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, float height) {

		Vector3[] vertices = new Vector3[8];

		vertices [0] = new Vector3 (p1.x, 0, p1.y);
		vertices [1] = new Vector3 (p2.x, 0, p2.y);
		vertices [2] = new Vector3 (p3.x, 0, p3.y);
		vertices [3] = new Vector3 (p4.x, 0, p4.y);
		vertices [4] = new Vector3 (p1.x, height, p1.y);
		vertices [5] = new Vector3 (p2.x, height, p2.y);
		vertices [6] = new Vector3 (p3.x, height, p3.y);
		vertices [7] = new Vector3 (p4.x, height, p4.y);

		int[] indices = new int[36];

		indices [0] = 0;
		indices [1] = 1;
		indices [2] = 2;
		indices [3] = 2;
		indices [4] = 1;
		indices [5] = 3;

		indices [6] = 4;
		indices [7] = 5;
		indices [8] = 6;
		indices [9] = 6;
		indices [10] = 5;
		indices [11] = 7;

		indices [12] = 0;
		indices [13] = 5;
		indices [14] = 4;
		indices [15] = 5;
		indices [16] = 0;
		indices [17] = 1;

		indices [18] = 0;
		indices [19] = 4;
		indices [20] = 6;
		indices [21] = 0;
		indices [22] = 6;
		indices [23] = 2;

		indices [24] = 2;
		indices [25] = 6;
		indices [26] = 7;
		indices [27] = 2;
		indices [28] = 7;
		indices [29] = 3;

		indices [30] = 3;
		indices [31] = 7;
		indices [32] = 5;
		indices [33] = 3;
		indices [34] = 5;
		indices [35] = 1;

		Mesh m = new Mesh ();

		m.vertices = vertices;
		m.triangles = indices;
		m.RecalculateNormals ();
		m.RecalculateBounds ();

		return m;
	}

	public bool containsPointInMesh(Vector2 point) {

		Vector3[] vertices = mesh.vertices;
		int[] triangles = mesh.triangles;

		for (int i = 0; i < triangles.Length; i += 3) {

			Vector3 tri1 = vertices [triangles [i]];
			Vector3 tri2 = vertices [triangles [i + 1]];
			Vector3 tri3 = vertices [triangles [i + 2]];

			Vector2 t1 = new Vector2 (tri1.x, tri1.z);
			Vector2 t2 = new Vector2 (tri2.x, tri2.z);
			Vector2 t3 = new Vector2 (tri3.x, tri3.z);

			if (pointInTriangle (point, t1, t2, t3)) {
				return true;
			}
		}
		return false;
	}

	public bool pointInTriangle(Vector2 pt, Vector2 tri1, Vector2 tri2, Vector2 tri3) {

		bool b1, b2, b3;

		b1 = sign(pt, tri1, tri2) < 0.0f;
		b2 = sign(pt, tri2, tri3) < 0.0f;
		b3 = sign(pt, tri3, tri1) < 0.0f;

		return ((b1 == b2) && (b2 == b3));
	}

	public float sign (Vector2 p1, Vector2 p2, Vector2 p3)
	{
		return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
	}


}
