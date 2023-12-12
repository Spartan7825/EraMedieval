using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuantumCookie
{
	class Utilities
	{
		// Given three collinear points p, q, r, the function checks if 
        // point q lies on line segment 'pr' 
        private static bool OnSegment(Vector3 p, Vector3 q, Vector3 r) 
        { 
        	if (q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) && 
        		q.y <= Mathf.Max(p.y, r.y) && q.y >= Mathf.Min(p.y, r.y)) 
		        return true; 
    
        	return false; 
        } 
    
        // To find orientation of ordered triplet (p, q, r). 
        // The function returns following values 
        // 0 --> p, q and r are collinear 
        // 1 --> Clockwise 
        // 2 --> Counterclockwise 
        private static int Orientation(Vector3 p, Vector3 q, Vector3 r) 
        { 
        	// See https://www.geeksforgeeks.org/orientation-3-ordered-points/ 
        	// for details of below formula. 
        	float val = (q.y - p.y) * (r.x - q.x) - 
        			(q.x - p.x) * (r.y - q.y); 
    
        	if (val == 0) return 0; // collinear 
    
        	return (val > 0)? 1: 2; // clock or counterclock wise 
        } 
    
        // The main function that returns true if line segment 'p1q1' 
        // and 'p2q2' intersect. 
        public static bool LineSegmentsIntersect(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
        {
	        p1.y = p1.z;
	        q1.y = q1.z;
	        p2.y = p2.z;
	        q2.y = q2.z;

	        p1.z = q1.z = p2.z = q2.z = 0;
	        
        	// Find the four orientations needed for general and 
        	// special cases 
        	int o1 = Orientation(p1, q1, p2); 
        	int o2 = Orientation(p1, q1, q2); 
        	int o3 = Orientation(p2, q2, p1); 
        	int o4 = Orientation(p2, q2, q1); 
    
        	// General case 
        	if (o1 != o2 && o3 != o4) 
        		return true; 
    
        	// Special Cases 
        	// p1, q1 and p2 are collinear and p2 lies on segment p1q1 
        	if (o1 == 0 && OnSegment(p1, p2, q1)) return true; 
    
        	// p1, q1 and q2 are collinear and q2 lies on segment p1q1 
        	if (o2 == 0 && OnSegment(p1, q2, q1)) return true; 
    
        	// p2, q2 and p1 are collinear and p1 lies on segment p2q2 
        	if (o3 == 0 && OnSegment(p2, p1, q2)) return true; 
    
        	// p2, q2 and q1 are collinear and q1 lies on segment p2q2 
        	if (o4 == 0 && OnSegment(p2, q1, q2)) return true; 
    
        	return false; // Doesn't fall in any of the above cases 
        }
	}
} 
