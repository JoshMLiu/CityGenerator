using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoastMap {

	public bool left, right, top, bottom;
	public bool topleft, topright, bottomleft, bottomright;

	public enum Location {
		LEFT,
		RIGHT,
		TOP,
		BOTTOM,
		TOPLEFT,
		TOPRIGHT,
		BOTTOMLEFT,
		BOTTOMRIGHT
	}

	public CoastMap() {
		left = false;
		right = false;
		top = false;
		bottom = false;
		topleft = false;
		topright = false;
		bottomleft = false;
		bottomright = false;
	}

	public void setSide(Location loc) {
		switch (loc) 
		{
		case Location.LEFT:
			left = true;
			topleft = false;
			bottomleft = false;
			break;
		case Location.RIGHT:
			right = true;
			topright = false;
			bottomright = false;
			break;
		case Location.TOP:
			top = true;
			topleft = false;
			topright = false;
			break;
		case Location.BOTTOM: 
			bottom = true;
			bottomleft = false;
			bottomright = false;
			break;
		case Location.TOPLEFT: 
			if (!left && !top)
				topleft = true;
			break;
		case Location.TOPRIGHT:
			if (!right && !top)
				topright = true;
			break;
		case Location.BOTTOMLEFT: 
			if (!left && !bottom)
				bottomleft = true;
			break;
		case Location.BOTTOMRIGHT:
			if (!right && !bottom)
				bottomright = true;
			break;
		default:
			break;
		}
	}

}
