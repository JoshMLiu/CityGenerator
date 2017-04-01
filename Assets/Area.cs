using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

public class Area {

	public static int coastsubdivisions = 6;
	int level;
	public Type type;
	public List<XmlNode> regionnodes;
	public CoastMap coastmap;
	public List<Mesh> meshes;
	public List<Subdivision> subdivisions;
	float streetwidth;

	public enum Type {
		LAND,
		WATER,
		COAST,
	}

	float minx;
	float miny;
	float maxx;
	float maxy;

	float halfx;
	float halfy;

	float width;
	float height;

	int[] sides;
	int[] corners;

	public Area(float mnx, float mny, float mxx, float mxy) {
		minx = mnx;
		miny = mny;
		maxx = mxx;
		maxy = mxy;

		width = maxx - minx;
		height = maxy - miny;

		halfx = (maxx + minx) / 2;
		halfy = (maxy + miny) / 2;

		type = Type.WATER;
		regionnodes = new List<XmlNode> ();

		coastmap = new CoastMap ();
		meshes = new List<Mesh> ();

		subdivisions = new List<Subdivision> ();
		streetwidth = width / 60;
	}

	public float getWidth() {
		return width;
	}

	public float getHeight() {
		return height;
	}

	public float getMinX() {
		return minx;
	}

	public float getMinY() {
		return miny;
	}

	public float getMaxX() {
		return maxx;
	}

	public float getMaxY() {
		return maxy;
	}

	public float getHalfX() {
		return halfx;
	}

	public float getHalfY() {
		return halfy;
	}

	public bool containsPointInMesh(Vector2 point) {

		foreach (Mesh m in meshes) {

			Vector3[] vertices = m.vertices;
			int[] triangles = m.triangles;

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

	public bool containsPoint(Vector2 point) {
		if (point.x >= minx && point.x <= maxx && point.y >= miny && point.y <= maxy) {
			return true;
		}
		return false;
	}

	public char typeChar() {
		if (type == Type.LAND)
			return 'L';
		else if (type == Type.COAST)
			return 'C';
		else 
			return 'W';
	}

	public Vector2[] randomCoast(Vector2 start, Vector2 end, bool corner) {

		List<Vector2> vertices = new List<Vector2>();
		vertices.Add (start);
		vertices.Add (end);

		for (int i = 0; i < coastsubdivisions; i++) {
			List<Vector2> newvertices = new List<Vector2> ();
			for (int j = 0; j < vertices.Count - 1; j++) {

				Vector2 curr = vertices [j];
				Vector2 next = vertices [j + 1];

				Vector2 midpoint = Vector2.Scale (new Vector2 (0.5f, 0.5f), (curr + next));
				Vector2 direction = next - curr;
				Vector2 normal = new Vector2 (-direction.y, direction.x);
				normal.Normalize ();

				float maxdist;
				float rand;
				if (corner) {
					maxdist = direction.magnitude / 9f;
					rand = Random.Range (-maxdist, maxdist);
				} else {
					maxdist = direction.magnitude / 5.1f;
					rand = Random.Range (-maxdist, maxdist);
				}

				Vector2 newvec = midpoint + Vector2.Scale (new Vector2 (rand, rand), normal);

				newvertices.Add (curr);
				newvertices.Add (newvec);

			}
			newvertices.Add(vertices[vertices.Count - 1]);
			vertices = newvertices;
		}
		return vertices.ToArray();
	}

	public void createMeshes() {

		Vector2 lefthalf = new Vector2 (minx, halfy);
		Vector2 righthalf = new Vector2 (maxx, halfy);
		Vector2 tophalf = new Vector2 (halfx, maxy);
		Vector2 bottomhalf = new Vector2 (halfx, miny);

		if (coastmap.left) {

			bool skip = false;
			if (coastmap.top) {

				Vector2[] randomline1 = randomCoast (righthalf, bottomhalf, true); 
				Vector2[] meshpoints1 = new Vector2[randomline1.Length + 3];

				for (int i = 0; i < meshpoints1.Length - 3; i++) {
					meshpoints1 [i] = randomline1 [i];
				}
				meshpoints1 [meshpoints1.Length - 3] = new Vector2 (minx, miny);
				meshpoints1 [meshpoints1.Length - 2] = new Vector2 (minx, maxy);
				meshpoints1 [meshpoints1.Length - 1] = new Vector2 (maxx, maxy);

				Mesh m1 = createMesh (meshpoints1);
				meshes.Add (m1);
				skip = true;

			}

			if (coastmap.bottom) {

				Vector2[] randomline2 = randomCoast (tophalf, righthalf, true); 
				Vector2[] meshpoints2 = new Vector2[randomline2.Length + 3];

				for (int i = 0; i < meshpoints2.Length - 3; i++) {
					meshpoints2 [i] = randomline2 [i];
				}
				meshpoints2 [meshpoints2.Length - 3] = new Vector2 (maxx, miny);
				meshpoints2 [meshpoints2.Length - 2] = new Vector2 (minx, miny);
				meshpoints2 [meshpoints2.Length - 1] = new Vector2 (minx, maxy);

				Mesh m2 = createMesh (meshpoints2);
				meshes.Add (m2);
				skip = true;

			}
			if (skip) {
				goto Skip;
			}

			Vector2[] randomline = randomCoast (tophalf, bottomhalf, false); 
			Vector2[] meshpoints = new Vector2[randomline.Length + 2];

			meshpoints [0] = new Vector2 (minx, maxy);
			for (int i = 1; i < meshpoints.Length - 1; i++) {
				meshpoints [i] = randomline [i - 1];
			}
			meshpoints [meshpoints.Length - 1] = new Vector2 (minx, miny);

			Mesh m = createMesh (meshpoints);
			meshes.Add (m);
		}

		if (coastmap.right) {

			bool skip = false;
			if (coastmap.top) {

				Vector2[] randomline1 = randomCoast (lefthalf, bottomhalf, true); 
				Vector2[] meshpoints1 = new Vector2[randomline1.Length + 3];

				for (int i = 0; i < meshpoints1.Length - 3; i++) {
					meshpoints1 [i] = randomline1 [i];
				}
				meshpoints1 [meshpoints1.Length - 3] = new Vector2 (maxx, miny);
				meshpoints1 [meshpoints1.Length - 2] = new Vector2 (maxx, maxy);
				meshpoints1 [meshpoints1.Length - 1] = new Vector2 (minx, maxy);

				Mesh m1 = createMesh (meshpoints1);
				meshes.Add (m1);
				skip = true;
			}

			if (coastmap.bottom) {

				Vector2[] randomline2 = randomCoast (lefthalf, tophalf, true); 
				Vector2[] meshpoints2 = new Vector2[randomline2.Length + 3];

				for (int i = 0; i < meshpoints2.Length - 3; i++) {
					meshpoints2 [i] = randomline2 [i];
				}
				meshpoints2 [meshpoints2.Length - 3] = new Vector2 (maxx, maxy);
				meshpoints2 [meshpoints2.Length - 2] = new Vector2 (maxx, miny);
				meshpoints2 [meshpoints2.Length - 1] = new Vector2 (minx, miny);

				Mesh m2 = createMesh (meshpoints2);
				meshes.Add (m2);
				skip = true;
			}
			if (skip) {
				goto Skip;
			}
				
			Vector2[] randomline = randomCoast (tophalf, bottomhalf, false); 
			Vector2[] meshpoints = new Vector2[randomline.Length + 2];

			meshpoints [0] = new Vector2 (maxx, maxy);
			for (int i = 1; i < meshpoints.Length - 1; i++) {
				meshpoints [i] = randomline [i - 1];
			}
			meshpoints [meshpoints.Length - 1] = new Vector2 (maxx, miny);

			Mesh m = createMesh (meshpoints);
			meshes.Add (m);
		}

		if (coastmap.top) {

			Vector2[] randomline = randomCoast (righthalf, lefthalf, false); 
			Vector2[] meshpoints = new Vector2[randomline.Length + 2];

			meshpoints [0] = new Vector2 (maxx, maxy);
			for (int i = 1; i < meshpoints.Length - 1; i++) {
				meshpoints [i] = randomline [i - 1];
			}
			meshpoints [meshpoints.Length - 1] = new Vector2 (minx, maxy);

			Mesh m = createMesh (meshpoints);
			meshes.Add (m);
		}

		if (coastmap.bottom) {

			Vector2[] randomline = randomCoast (righthalf, lefthalf, false); 
			Vector2[] meshpoints = new Vector2[randomline.Length + 2];

			meshpoints [0] = new Vector2 (maxx, miny);
			for (int i = 1; i < meshpoints.Length - 1; i++) {
				meshpoints [i] = randomline [i - 1];
			}
			meshpoints [meshpoints.Length - 1] = new Vector2 (minx, miny);

			Mesh m = createMesh (meshpoints);
			meshes.Add (m);
		}

	Skip:
		if (coastmap.topleft) {

			Vector2[] randomline = randomCoast (tophalf, lefthalf, true); 
			Vector2[] meshpoints = new Vector2[randomline.Length + 1];

			meshpoints [0] = new Vector2 (minx, maxy);
			for (int i = 1; i < meshpoints.Length; i++) {
				meshpoints [i] = randomline [i - 1];
			}

			Mesh m = createMesh (meshpoints);
			meshes.Add (m);
		}

		if (coastmap.topright) {

			Vector2[] randomline = randomCoast (tophalf, righthalf, true); 
			Vector2[] meshpoints = new Vector2[randomline.Length + 1];

			meshpoints [0] = new Vector2 (maxx, maxy);
			for (int i = 1; i < meshpoints.Length; i++) {
				meshpoints [i] = randomline [i - 1];
			}

			Mesh m = createMesh (meshpoints);
			meshes.Add (m);
		}

		if (coastmap.bottomleft) {

			Vector2[] randomline = randomCoast (lefthalf, bottomhalf, true); 
			Vector2[] meshpoints = new Vector2[randomline.Length + 1];

			meshpoints [0] = new Vector2 (minx, miny);
			for (int i = 1; i < meshpoints.Length; i++) {
				meshpoints [i] = randomline [i - 1];
			}

			Mesh m = createMesh (meshpoints);
			meshes.Add (m);
		}

		if (coastmap.bottomright) {

			Vector2[] randomline = randomCoast (righthalf, bottomhalf, true); 
			Vector2[] meshpoints = new Vector2[randomline.Length + 1];

			meshpoints [0] = new Vector2 (maxx, miny);
			for (int i = 1; i < meshpoints.Length; i++) {
				meshpoints [i] = randomline [i - 1];
			}

			Mesh m = createMesh (meshpoints);
			meshes.Add (m);
		}

	}

	public Mesh createMesh(Vector2[] meshpoints) {
		Triangulator tr = new Triangulator (meshpoints);
		int[] indices = tr.Triangulate ();

		Vector3[] vertices = new Vector3[meshpoints.Length];
		for (int i = 0; i < vertices.Length; i++) {
			vertices [i] = new Vector3(meshpoints [i].x, 0, meshpoints [i].y);
		}
		Mesh mesh = new Mesh (); 
		mesh.vertices = vertices;
		mesh.triangles = indices;
		mesh.RecalculateNormals ();
		mesh.RecalculateBounds ();
		return mesh;
	}
		
	public void setRandomSubdivisions() {

		Vector2 topleft = new Vector2 (minx, maxy);
		Vector2 topright = new Vector2 (maxx, maxy);
		Vector2 bottomleft = new Vector2 (minx, miny);
		Vector2 bottomright = new Vector2 (maxx, miny);

		int xdivisions = Random.Range (2, 4);
		int ydivisions = Random.Range (2, 4);

		Vector2[,] intersections = new Vector2[xdivisions + 2, ydivisions + 2];
		intersections [0, 0] = topleft;
		intersections [xdivisions + 1, 0] = topright;
		intersections [0, ydivisions + 1] = bottomleft;
		intersections [xdivisions + 1, ydivisions + 1] = bottomright;

		// top and bottom random points
		for (int i = 1; i <= xdivisions; i++) {

			float minxpointtop = minx + ((float)(i - 1) / xdivisions) * width;
			float maxxpointtop = minx + ((float)i / xdivisions) * width;


			float randxtop = Random.Range (minxpointtop, maxxpointtop);
			float randxbot = Random.Range (minxpointtop, maxxpointtop);

			intersections [i, 0] = new Vector2 (randxtop, maxy);
			intersections [i, ydivisions + 1] = new Vector2 (randxbot, miny);

		}

		// left and right random points
		for (int i = 1; i <= ydivisions; i++) {

			float minypointleft = maxy - ((float)(i - 1) / ydivisions) * height;
			float maxypointleft = maxy - ((float)i / ydivisions) * height;


			float randxleft = Random.Range (minypointleft, maxypointleft);
			float randxright = Random.Range (minypointleft, maxypointleft);

			intersections [0, i] = new Vector2 (minx, randxleft);
			intersections [xdivisions + 1, i] = new Vector2 (maxx, randxright);

		}

		// get intersections between lines

		for (int i = 1; i <= xdivisions; i++) {
			for (int j = 1; j <= ydivisions; j++) {

				Vector2 vertlinepoint1 = intersections [i, 0];
				Vector2 vertlinepoint2 = intersections [i, ydivisions + 1];
				Vector2 vertdirection = (vertlinepoint2 - vertlinepoint1);

				Vector2 horlinepoint1 = intersections [0, j];
				Vector2 horlinepoint2 = intersections [xdivisions + 1, j];
				Vector2 hordirection = (horlinepoint2 - horlinepoint1);

				Vector3 vertorigin = new Vector3 (vertlinepoint1.x, vertlinepoint1.y, 0);
				Vector3 hororigin = new Vector3 (horlinepoint1.x, horlinepoint1.y, 0);
				Vector3 vertline = new Vector3 (vertdirection.x, vertdirection.y, 0);
				Vector3 horline = new Vector3 (hordirection.x, hordirection.y, 0);

				Vector3 resultpoint = new Vector3 ();

				Math3d.LineLineIntersection (out resultpoint, vertorigin, vertline, hororigin, horline);

				Vector2 result2d = new Vector2 (resultpoint.x, resultpoint.y);

				intersections [i, j] = result2d;

			}
		}

		// print 
		System.Console.WriteLine(minx + " " + maxx+ " " + miny + " " + maxy + " " + width + " " + height);
		for (int j = 0; j <= ydivisions + 1; j++) {
			string s = "";
			for (int i = 0; i <= xdivisions + 1; i++) {
				s += intersections [i, j] + " ";
			}
			System.Console.WriteLine(s);
		}

		for (int j = 0; j <= ydivisions; j++) {
			for (int i = 0; i <= xdivisions; i++) {

				Vector2 tl = intersections [i, j];
				Vector2 tr = intersections [i + 1, j];
				Vector2 bl = intersections [i, j + 1];
				Vector2 br = intersections [i + 1, j + 1];

				Subdivision sub = new Subdivision (tl, tr, bl, br, streetwidth);
				subdivisions.Add (sub);
			}
		}

	}
}
