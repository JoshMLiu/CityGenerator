using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Contained within an area, has an irregular quad shape
public class Subdivision {

	public Mesh mesh;
	public Vector3[] points;
	float streetwidth;
	public Statistics stats;
	public Vector2 midpoint;
	// Too small for buildings
	public bool isempty;

	public Subdivision(Vector2 topleft, Vector2 topright, Vector2 botleft, Vector2 botright, float sw) {

		points = new Vector3[4];
		streetwidth = sw;

		points [0] = new Vector3(topleft.x + streetwidth, 0, topleft.y - streetwidth);
		points [1] = new Vector3(topright.x - streetwidth, 0, topright.y - streetwidth);
		points [2] = new Vector3(botleft.x + streetwidth, 0, botleft.y + streetwidth);
		points [3] = new Vector3 (botright.x - streetwidth, 0, botright.y + streetwidth);

		float midpointx = ((points [0].x + points [3].x) / 2 + (points [1].x + points [2].x) / 2) / 2;
		float midpointy = ((points [0].y + points [3].y) / 2 + (points [1].y + points [2].y) / 2) / 2;
		midpoint = new Vector2 (midpointx, midpointy);

		mesh = new Mesh ();
		createMesh ();
		isempty = false;

		// Check for size
		if (topright.x - topleft.x <= 3*streetwidth) {
			isempty = true;
			return;
		}
		if (botright.x - botleft.x <= 3*streetwidth) {
			isempty = true;
			return;
		}
		if (topleft.y - botleft.y <= 3*streetwidth) {
			isempty = true;
			return;
		}
		if (topright.y - botright.y <= 3*streetwidth) {
			isempty = true;
			return;
		}
			
		stats = new Statistics ();
	}

	// creates a quad mesh plane
	void createMesh() {

		int[] indices = new int[6];

		// firsttriangle
		indices [0] = 0;
		indices [1] = 1;
		indices [2] = 2;
		// secondtriangle
		indices [3] = 2;
		indices [4] = 1;
		indices [5] = 3;
 
		mesh.vertices = points;
		mesh.triangles = indices;
		mesh.RecalculateNormals ();
		mesh.RecalculateBounds ();
	}

	public List<MeshPack> getBuildingMeshes(float outputchance) {

		List<MeshPack> meshes = new List<MeshPack> ();

		// slight offset to move buildings away from road
		Vector3[] temppoints = new Vector3[4];
		temppoints [0] = points [0] + new Vector3 (streetwidth / 2, 0, -streetwidth / 2);
		temppoints [1] = points [1] + new Vector3 (-streetwidth / 2, 0, -streetwidth / 2);
		temppoints [2] = points [2] + new Vector3 (streetwidth / 2, 0, streetwidth / 2);
		temppoints [3] = points [3] + new Vector3 (-streetwidth / 2, 0, streetwidth / 2);

		// Get minimum bounding box
		float minx = Mathf.Min (temppoints [0].x, temppoints [2].x);
		float maxx = Mathf.Max (temppoints [1].x, temppoints [3].x);

		float miny = Mathf.Min (temppoints [2].z, temppoints [3].z);
		float maxy = Mathf.Max (temppoints [0].z, temppoints [1].z);

		float currentx = minx;
		float currenty = maxy;

		// iterate in building sizes (random) over the bounding box
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

				// midpoint of building is on road, don't render the building
				if (containsPointInMesh (midpoint) && lengthx > randx / 2 && lengthy > randy / 2) {

					Vector2 tl = new Vector2 (currentx, currenty);
					Vector2 tr = new Vector2 (nextx, currenty);
					Vector2 bl = new Vector2 (currentx, nexty);
					Vector2 br = new Vector2 (nextx, nexty);

					Vector2 newtl = tl;
					Vector2 newtr = tr;
					Vector2 newbl = bl;
					Vector2 newbr = br;

					// tower, food, shop etc...
					Statistics.BuildingType btype = stats.getRandomBuilding ();

					// Check if each corner sticks out from subdivision and crop appropriately
					if (!containsPointInMesh (tl)) {

						Vector3 intersectionpoint = new Vector3 ();
						Vector2 line1dir = new Vector3 (tr.x, 0, tr.y) - new Vector3 (tl.x, 0, tl.y);
						Math3d.LineLineIntersection (out intersectionpoint, new Vector3 (tl.x, 0, tl.y), line1dir, temppoints [0], temppoints [2] - temppoints [0]);
						Vector2 point = new Vector2 (intersectionpoint.x + 0.001f, intersectionpoint.z);
						if (containsPointInMesh (point) && tl.x <= point.x) {
							newtl = point;
						} else {
							Vector2 point2 = Math3d.LineIntersectionPoint (new Vector2(temppoints [0].x, temppoints[0].z), new Vector2(temppoints [1].x, temppoints[1].z), tl, bl);
							point2 = point2 + new Vector2(0, -0.001f);
							if (containsPointInMesh (point2) && tl.y >= point2.y) {
								newtl = point2;
							}
						}

					}
					if (!containsPointInMesh (tr)) {
						Vector3 intersectionpoint = new Vector3 ();
						Vector2 line1dir = new Vector3 (tl.x, 0, tl.y) - new Vector3 (tr.x, 0, tr.y);
						Math3d.LineLineIntersection (out intersectionpoint, new Vector3 (tr.x, 0, tr.y), line1dir, temppoints [1], temppoints [3] - temppoints [1]);
						Vector2 point = new Vector2 (intersectionpoint.x - 0.001f, intersectionpoint.z);
						if (containsPointInMesh (point) && tr.x >= point.x) {
							newtr = point;
						} else {
							Vector2 point2 = Math3d.LineIntersectionPoint (new Vector2(temppoints [1].x, temppoints[1].z), new Vector2(temppoints [0].x, temppoints[0].z), tr, br);
							point2 = point2 + new Vector2(0, -0.001f);
							if (containsPointInMesh (point2) && tr.y >= point2.y) {
								newtr = point2;
							}
						} 

					}
					if (!containsPointInMesh (bl)) {
						Vector3 intersectionpoint = new Vector3 ();
						Vector2 line1dir = new Vector3 (br.x, 0, br.y) - new Vector3 (bl.x, 0, bl.y);
						Math3d.LineLineIntersection (out intersectionpoint, new Vector3 (bl.x, 0, bl.y), line1dir, temppoints [2], temppoints [0] - temppoints [2]);
						Vector2 point = new Vector2 (intersectionpoint.x + 0.001f, intersectionpoint.z);
						if (containsPointInMesh (point) && bl.x <= point.x) {
							newbl = point;
						} else {
							Vector2 point2 = Math3d.LineIntersectionPoint (new Vector2(temppoints [2].x, temppoints[2].z), new Vector2(temppoints [3].x, temppoints[3].z), bl, tl);
							point2 = point2 + new Vector2(0, +0.001f);
							if (containsPointInMesh (point2) && bl.y <= point2.y) {
								newbl = point2;
							}
						} 

					}
					if (!containsPointInMesh (br)) {
						Vector3 intersectionpoint = new Vector3 ();
						Vector2 line1dir = new Vector3 (bl.x, 0, bl.y) - new Vector3 (br.x, 0, br.y);
						Math3d.LineLineIntersection (out intersectionpoint, new Vector3 (br.x, 0, br.y), line1dir, temppoints [3], temppoints [1] - temppoints [3]);
						Vector2 point = new Vector2 (intersectionpoint.x - 0.001f, intersectionpoint.z);
						if (containsPointInMesh (point) && br.x >= point.x) {
							newbr = point;
						} else {
							Vector2 point2 = Math3d.LineIntersectionPoint (new Vector2(temppoints [3].x, temppoints[3].z), new Vector2(temppoints [2].x, temppoints[2].z), br, tr);
							point2 = point2 + new Vector2(0, +0.001f);
							if (containsPointInMesh (point2) && br.y <= point2.y) {
								newbr = point2;
							}
						} 

					}
					// Special cropping case where the corner sticks out on the x and y
					if (pointInTriangle (new Vector2 (temppoints [0].x, temppoints [0].z), tl, tr, bl) || pointInTriangle (new Vector2 (temppoints [0].x, temppoints [0].z), bl, tr, br)) {
						newtl = new Vector2 (temppoints [0].x, temppoints [0].z);
					}
					if (pointInTriangle (new Vector2 (temppoints [1].x, temppoints [1].z), tl, tr, bl) || pointInTriangle (new Vector2 (temppoints [1].x, temppoints [1].z), bl, tr, br)) {
						newtr = new Vector2 (temppoints [1].x, temppoints [1].z);
					}
					if (pointInTriangle (new Vector2 (temppoints [2].x, temppoints [2].z), tl, tr, bl) || pointInTriangle (new Vector2 (temppoints [2].x, temppoints [2].z), bl, tr, br)) {
						newbl = new Vector2 (temppoints [2].x, temppoints [2].z);
					}	
					if (pointInTriangle (new Vector2 (temppoints [3].x, temppoints [3].z), tl, tr, bl) || pointInTriangle (new Vector2 (temppoints [3].x, temppoints [3].z), bl, tr, br)) {
						newbr = new Vector2 (temppoints [3].x, temppoints [3].z);
					}

					tl = newtl;
					tr = newtr;
					bl = newbl;
					br = newbr;

					// Set small spaces between buildings
					Vector2 reducedtl = tl + new Vector2 (streetwidth / 4, -streetwidth / 4);
					Vector2 reducedtr = tr + new Vector2 (-streetwidth / 4, -streetwidth / 4);
					Vector2 reducedbl = bl + new Vector2 (streetwidth / 4, streetwidth / 4);
					Vector2 reducedbr = br + new Vector2 (-streetwidth / 4, streetwidth / 4);

					float maxwidth = Mathf.Max (reducedtr.x - reducedtl.x, reducedbr.x - reducedbl.x);
					float maxheight = Mathf.Max (reducedtl.y - reducedbl.y, reducedtr.y - reducedbr.y);

					// Don't render if building is too skinny
					if (maxwidth < streetwidth || maxheight < streetwidth) {
						currentx = nextx;
						continue;
					}

					if (btype == Statistics.BuildingType.TOWER) {
						float rand = Random.Range (0f, 1f);
						if (rand <= outputchance) {
							// Get random tower height based on avg height and variance
							float h = (float)stats.getRandomTowerHeight ();
							MeshPack mp = new MeshPack (createBuildingMesh (reducedtl, reducedtr, reducedbl, reducedbr, h / 10 + 0.2f));
							mp.btype = btype;
							meshes.Add(mp);
						}

					} else {
						float rand = Random.Range (0f, 1f);
						if (rand <= outputchance) {
							float randheight = Random.Range (0.4f, 1f);
							MeshPack mp = new MeshPack (createBuildingMesh (reducedtl, reducedtr, reducedbl, reducedbr, randheight));
							mp.btype = btype;
							meshes.Add (mp);
						}
					}		
				}
				currentx = nextx;
			}
			currenty = nexty;
		}

		return meshes;
	}

	public Mesh getOtherMesh(Statistics.OtherType otype) {

		if (otype == Statistics.OtherType.LEISURE || otype == Statistics.OtherType.ATTRACTION) {

			// mesh fills whole subdivision
			float randheight = Random.Range (0.5f, 0.8f);
			Vector2 tl = new Vector2 (points [0].x + streetwidth/2, points [0].z - streetwidth/2);
			Vector2 tr = new Vector2 (points [1].x - streetwidth/2, points [1].z - streetwidth/2);
			Vector2 bl = new Vector2 (points [2].x + streetwidth/2, points [2].z + streetwidth/2);
			Vector2 br = new Vector2 (points [3].x - streetwidth/2, points [3].z + streetwidth/2);
			return createBuildingMesh (tl, tr, bl, br, randheight);

		} else if (otype == Statistics.OtherType.FUEL) {

			// Mesh fills a portion of subdivision on random side (like a gas station)
			int randside = Random.Range (0, 4);

			if (randside == 0) {
				// left

				float dist = Mathf.Min ((points [0] - points [1]).magnitude, (points [2] - points [3]).magnitude);
				if (dist < 1) {
					return new Mesh ();
				}

				float randlength = dist/2 + Random.Range(-0.1f, 0.1f);
				Vector2 tl = new Vector2(points [0].x, points[0].z);
				Vector2 temptr = new Vector2 (points [1].x, points [1].z);
				Ray r1 = new Ray (tl, (temptr - tl).normalized);
				Vector2 tr = r1.GetPoint(randlength);
				Vector2 bl = new Vector2 (points [2].x, points [2].z);
				Vector2 tempbr = new Vector2 (points [3].x, points [3].z);
				Ray r2 = new Ray (bl, (tempbr - bl).normalized);
				Vector2 br = r2.GetPoint (randlength);

				float randheight = Random.Range (0.5f, 0.8f);
				return createBuildingMesh (tl, tr, bl, br, randheight);
			} else if (randside == 1) {
				// right
				float dist = Mathf.Min ((points [0] - points [1]).magnitude, (points [2] - points [3]).magnitude);
				if (dist < 1) {
					return new Mesh ();
				}

				float randlength = dist/2 + Random.Range(-0.1f, 0.1f);
				Vector2 tr = new Vector2(points [1].x, points[1].z);
				Vector2 temptl = new Vector2 (points [0].x, points [0].z);
				Ray r1 = new Ray (tr, (temptl - tr).normalized);
				Vector2 tl = r1.GetPoint(randlength);
				Vector2 br = new Vector2(points [3].x, points[3].z);
				Vector2 tempbl = new Vector2 (points [2].x, points [2].z);
				Ray r2 = new Ray (br, (tempbl - br).normalized);
				Vector2 bl = r2.GetPoint (randlength);

				float randheight = Random.Range (0.5f, 0.8f);
				return createBuildingMesh (tl, tr, bl, br, randheight);
			} else if (randside == 2) {
				// top
				float dist = Mathf.Min ((points [0] - points [2]).magnitude, (points [1] - points [3]).magnitude);
				if (dist < 1) {
					return new Mesh ();
				}

				float randlength = dist/2 + Random.Range(-0.1f, 0.1f);
				Vector2 tl = new Vector2(points [0].x, points[0].z);
				Vector2 tempbl = new Vector2 (points [2].x, points [2].z);
				Ray r1 = new Ray (tl, (tempbl - tl).normalized);
				Vector2 bl = r1.GetPoint(randlength);
				Vector2 tr = new Vector2(points [1].x, points[1].z);
				Vector2 tempbr = new Vector2 (points [3].x, points [3].z);
				Ray r2 = new Ray (tr, (tempbr - tr).normalized);
				Vector2 br = r2.GetPoint (randlength);

				float randheight = Random.Range (0.5f, 0.8f);
				return createBuildingMesh (tl, tr, bl, br, randheight);
			} else if (randside == 3) {
				// bot
				float dist = Mathf.Min ((points [0] - points [2]).magnitude, (points [1] - points [3]).magnitude);
				if (dist < 1) {
					return new Mesh ();
				}

				float randlength = dist/2 + Random.Range(-0.1f, 0.1f);
				Vector2 bl = new Vector2(points [2].x, points[2].z);
				Vector2 temptl = new Vector2 (points [0].x, points [0].z);
				Ray r1 = new Ray (bl, (temptl - bl).normalized);
				Vector2 tl = r1.GetPoint(randlength);
				Vector2 br = new Vector2(points [3].x, points[3].z);
				Vector2 temptr = new Vector2 (points [1].x, points [1].z);
				Ray r2 = new Ray (br, (temptr - br).normalized);
				Vector2 tr = r2.GetPoint (randlength);

				float randheight = Random.Range (0.5f, 0.8f);
				return createBuildingMesh (tl, tr, bl, br, randheight);
			}

		}
		return new Mesh ();

	}

	// Creates all triangles in 3d building mesh
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

		// bot
		indices [0] = 0;
		indices [1] = 1;
		indices [2] = 2;
		indices [3] = 2;
		indices [4] = 1;
		indices [5] = 3;

		//top
		indices [6] = 4;
		indices [7] = 5;
		indices [8] = 6;
		indices [9] = 6;
		indices [10] = 5;
		indices [11] = 7;

		//sides
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

	// checks if a point is in the subdivision
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
