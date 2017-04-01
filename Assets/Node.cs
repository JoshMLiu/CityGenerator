using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node {

	string id;
	float lat;
	float lon;

	public Node(string i, float lt, float ln) {
		id = i;
		lat = lt;
		lon = ln;
	}

	public string Id {
		get { return id; }
		set { id = value; }
	}

	public float Lat {
		get { return lat; }
		set { lat = value; }
	}

	public float Lon {
		get { return lon; }
		set { lon = value; }
	}
}
