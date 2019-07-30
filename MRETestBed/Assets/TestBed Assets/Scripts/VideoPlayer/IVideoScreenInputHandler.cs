using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IVideoScreenInputHandler
{
    // All of the methods handle input as a ratio between the min and max on a given axis of a quad.
    // For example, the bottom left corner of a quad is (0,0), and the bottom right is (1, 0). 
    // This is so that the caller does not have to start digging in to things like what the stream's
    // video width/height are, etc, and all they need to know is the publicly available height and width of the quad.
    void HandleSelectDown(float xRatio, float yRatio);
    void HandleSelectDrag(float xRatio, float yRatio);
    void HandleSelectUp(float xRatio, float yRatio);
}
