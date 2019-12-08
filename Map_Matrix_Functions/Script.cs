    public Program() 
{ 

} 
 
public void Save() 
{ 

} 
 
public void Main(string argument, UpdateType updateSource) 
{

   // double[,] testMatrix = new double[,] { {60,0,0,1}, {-60,0,0,1}, {0,60,0,1}, {0,0,60,1} };
   // double detTest = Det4(testMatrix);
   // Echo("Test Determinant: \n" + detTest.ToString());

    // Dummy Coordinates
    double[] coord1 = new double[] {60000,0,0};
    double t1= TValue(coord1);

    double[] coord2 = new double[] {-60000,0,0};
    double t2= TValue(coord2);

    double[] coord3 = new double[] {0, 60000,0};
    double t3= TValue(coord3);

    double[] coord4 = new double[] {0,0,60000};
    double t4= TValue(coord4);

    double[] arrT = new double[] {t1,t2,t3,t4};

    //Build matrixT with coords 1,2,3 & 4, and a column of 1's
    double[,] matrixT = new double [4,4];
    for(int c = 0; c<3; c++)
    {
        matrixT[0,c] = coord1[c];
    }
    for(int d = 0; d<3; d++)
    {
        matrixT[1,d] = coord2[d];
    }
    for(int e = 0; e<3; e++)
    {
        matrixT[2,e] = coord3[e];
    }
    for(int f = 0; f<3; f++)
    {
        matrixT[3,f] = coord4[f];
    }
    for(int g = 0; g<4; g++)
    {
        matrixT[g,3] = 1;
    }

    Echo("\nMatrix T");
    EchoMatrix(matrixT);
    Echo("Determinant: " + Det4(matrixT).ToString());

    double[,] matrixD = new double [4,4];
    ReplaceColumn(matrixT, matrixD, arrT, 0);
    Echo("\nMatrix D");
    EchoMatrix(matrixD);
    Echo("Determinant: " + Det4(matrixD).ToString());


    double[,] matrixE = new double [4,4];
    ReplaceColumn(matrixT, matrixE, arrT, 1);
    Echo("\nMatrix E");
    EchoMatrix(matrixE);
    Echo("Determinant: " + Det4(matrixE).ToString());

    double[,] matrixF = new double [4,4];
    ReplaceColumn(matrixT, matrixF, arrT, 2);
    Echo("\nMatrix F");
    EchoMatrix(matrixF);
    Echo("Determinant: " + Det4(matrixF).ToString());

    double[,] matrixG = new double [4,4];
    ReplaceColumn(matrixT, matrixG, arrT, 3);
    Echo("\nMatrix G");
    EchoMatrix(matrixG);
    Echo("Determinant: " + Det4(matrixG).ToString());

    double detT = Det4(matrixT);
    double detD = Det4(matrixD)/detT;
    double detE = Det4(matrixE)/detT;
    double detF = Det4(matrixF)/detT;
    double detG = Det4(matrixG)/detT;

    double[] center = new double[3];
    center[0] = detD/-2;
    center[1] = detE/-2;
    center[2] = detF/-2;

    double radius = Math.Sqrt(detD*detD + detE*detE + detF*detF - 4*detG)/2;

    Echo("\nPlanet Center: (" + center[0] + ", " + center[1] + ", " + center[2] + ")");
    Echo("Planet Radius: " + radius);



} 

void ReplaceColumn(double[,] matrix1, double[,] matrix2, double[] t, int column)
{
    for (int i = 0; i<4; i++)
    {
        for(int j = 0; j<4; j++)
        {
            matrix2[i,j] = matrix1[i,j];
        }
    }
    matrix2[0,column] = t[0];
    matrix2[1,column] = t[1];
    matrix2[2,column] = t[2];
    matrix2[3,column] = t[3];
}

double TValue(double[] coord)
{
    double result;
    result = -1*(coord[0]*coord[0] + coord[1]*coord[1] + coord[2]*coord[2]);
    return result;
}


    ///////////////////
  ///  EchoMatrix  ///          Prints Matrix to Terminal
///////////////////

void EchoMatrix(double[,] matrix)
{
    for(int row = 0; row<4; row++)
    {
            Echo(matrix[row,0] + " " + matrix[row,1] + " " + matrix[row,2] + " " + matrix[row,3]);
    }
}


    /////////////
  ///  Det3  ///          Gets determinant of a 3x3 Matrix
/////////////

double Det3(double[,] m)
{
    double determinant;

    determinant = m[0,0] * ( m[1,1] * m[2,2] - m[1,2] * m[2,1] ) - m[0,1] * ( m[1,0] * m[2,2] - m[1,2] * m[2,0] ) + m[0,2] * ( m[1,0] * m[2,1] - m[1,1] * m[2,0] );

    return determinant;
}



    /////////////
  ///  Det4  ///          Gets determinant of a 4x4 Matrix
/////////////

double Det4(double[,] m)
{
    double determinant;

    double[,] mA = new double[,] { {m[1,1], m[1,2], m[1,3]}, {m[2,1], m[2,2], m[2,3]}, {m[3,1], m[3,2], m[3,3]} };
    double[,] mB = new double[,] { {m[1,0], m[1,2], m[1,3]}, {m[2,0], m[2,2], m[2,3]}, {m[3,0], m[3,2], m[3,3]} };
    double[,] mC = new double[,] { {m[1,0], m[1,1], m[1,3]}, {m[2,0], m[2,1], m[2,3]}, {m[3,0], m[3,1], m[3,3]} };
    double[,] mD = new double[,] { {m[1,0], m[1,1], m[1,2]}, {m[2,0], m[2,1], m[2,2]}, {m[3,0], m[3,1], m[3,2]} };

    determinant = m[0,0] * Det3(mA) - m[0,1]*Det3(mB) + m [0,2]*Det3(mC) - m[0,3]*Det3(mD);

    return determinant;
}