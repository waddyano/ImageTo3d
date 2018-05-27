# ImageTo3d
Command line lithophane creator to create STL from images for 3d printing. 

Current help output is below.

    ImageTo3D: converts an image file to a 3d STL file.
               by default thickness of the model depends on darkness of the image thicker being darker

        ImageTo3D [-b] [-t] [-n] [-mx] [-my] [-w <width-in-mm>] [-minthick <thick-in-mm>]
                  [-noborder] [-borderthick <value-in-mm>] [-borderwidth <value-in-mm>]
                  [-maxthick <thick-in-mm>] <image-file> [<output-stl-file>]

           -b              set output format to binary (default)
           -t              set output format to text
           -n              use the negative image
           -mx             mirror image in X
           -my             mirror image in Y
           -w              set desired width (default 100)
           -noborder       do not add border round image
           -borderthick    thickness of border in millimeters (default 5)
           -borderwidth    width of border in millimeters (default 4)
           -minthick       set minimum thickness in millimeters (default 0.5)
           -maxthick       set mmaximum thickness in millimeters (default 3.5)
