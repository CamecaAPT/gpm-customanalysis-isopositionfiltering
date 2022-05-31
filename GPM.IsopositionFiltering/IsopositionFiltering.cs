using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Interface.Resources.IonData;
using Cameca.CustomAnalysis.Utilities;
using Microsoft.Win32.SafeHandles;

namespace GPM.CustomAnalysis.IsopositionFiltering;

internal class IsopositionFiltering
{


	// Parameters 
	// -------------
	private string Name_Int = "AM";                   // Name of the element of interest
	private double Grid_size_init = 1;                // Grid size in nm
	private double Grid_delo = 0.5;                   // Delocalization in nm
	private double X_threshold = 1;                   // Concentration threshold : Min
	private double X_step = 0.1;                      // Concentration distribution visualization : bin size
	private int Disp_step = 20;                           // Number of Iteration for the progress "bar"


	// Varaible declaration
	// ----------------------
	List<float> mass = new List<float>();
	List<Vector3> pos_init = new List<Vector3>();
	List<byte> name = new List<byte>();
	int Name_Id;


	public FrequencyDistributionComparisonData Run(IIonData ionData, IsopositionFilteringOptions options)
	{

		// Conversion US-FR
		// ---------------------
		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");



		Name_Int = options.Name_Int;                   // Name of the element of interest
		Grid_size_init = options.Grid_size_init;
		Grid_delo = options.Grid_delo;
		X_threshold = options.X_threshold;
		X_step = options.X_step;
		Disp_step = options.Disp_step;

		// Open Console
		// ---------------
		ConsoleHelper.AllocConsole();
		vPrepareConsole();
		Console.WriteLine("		");
		Console.WriteLine("			CLUSTER IDENTIFICATION : Isoposition filtering	");
		Console.WriteLine("		");


		// Display parameters
		// ---------------------	
		Console.WriteLine("Isoposition parameters :");
		Console.WriteLine("   Chemical element  : {0}", Name_Int);
		Console.WriteLine("   Concentration threshold (ion %)  = {0}", X_threshold);
		Console.WriteLine("		");
		Console.WriteLine("Computation parameters :");
		Console.WriteLine("   Grid size (nm)  = {0}", Grid_size_init);
		Console.WriteLine("   Delocalization (nm)  = {0}", Grid_delo);
		Console.WriteLine("		");
		Console.WriteLine("Visualization parameters :");
		Console.WriteLine("   Histogram step (ion %)  = {0}", X_step);


		// data extraction
		// -----------------
		//var ionData = parentNode.IonData;
		var sections = new string[]
		{
			IonDataSectionName.Mass,
			IonDataSectionName.Position,
			IonDataSectionName.IonType,
		};
		foreach (var chunk in ionData.CreateSectionDataEnumerable(sections))
		{
			var masses = chunk.ReadSectionData<float>(IonDataSectionName.Mass);
			var positions = chunk.ReadSectionData<Vector3>(IonDataSectionName.Position);
			var ionTypes = chunk.ReadSectionData<byte>(IonDataSectionName.IonType);

			for (int index = 0; index < chunk.Length; index++)
			{
				mass.Add(masses.Span[index]);
				pos_init.Add(positions.Span[index]);
				name.Add(ionTypes.Span[index]);
			}
		}


		// Min and max value in X, Y and Z position.
		// ---------------------------------------------
		float xMin = float.MaxValue;
		float xMax = float.MinValue;
		float yMin = float.MaxValue;
		float yMax = float.MinValue;
		float zMin = float.MaxValue;
		float zMax = float.MinValue;

		int pos_length = 0;
		foreach (Vector3 p in pos_init)
		{
			if (p.X < xMin)
				xMin = p.X;
			if (p.X > xMax)
				xMax = p.X;
			if (p.Y < yMin)
				yMin = p.Y;
			if (p.Y > yMax)
				yMax = p.Y;
			if (p.Z < zMin)
				zMin = p.Z;
			if (p.Z > zMax)
				zMax = p.Z;
			pos_length = pos_length + 1;
		}


		// Repositionning the tip
		// -------------------------
		float[,] pos = new float[pos_length, 3];

		int ite;
		for (ite = 0; ite < pos_length; ite++)
		{
			pos[ite, 0] = pos_init[ite].X - xMin;
			pos[ite, 1] = pos_init[ite].Y - yMin;
			pos[ite, 2] = pos_init[ite].Z - zMin;
		}


		// Generate random distribution
		// ---------------------------------
		float[] mass_rnd = new float[pos_length];
		byte[] name_rnd = new byte[pos_length];
		for (ite = 0; ite < pos_length; ite++)
		{
			int test = Convert.ToInt32(GetRandomNumber(0, (pos_length - 1)));
			mass_rnd[test] = mass[ite];
			name_rnd[test] = name[ite];
			mass_rnd[ite] = mass[test];
			name_rnd[ite] = name[test];
		}


		//  Name of impacts : Define the element of interest
		// ------------------------------------------------------
		List<string> impact_name = ionData.GetIonTypeCounts().Keys.Select(o => o.Name).ToList();
		int listLen = impact_name.Count;

		for (int iName = 0; iName < listLen; iName++)
		{
			if (impact_name[iName] == Name_Int)
			{
				Name_Id = iName;
			}
		}


		// 3D grid : Definition and resizing
		// -----------------------------------
		int x_grid = Convert.ToInt32(((xMax - xMin) / Grid_size_init) + 1);
		int y_grid = Convert.ToInt32(((yMax - yMin) / Grid_size_init) + 1);
		int z_grid = Convert.ToInt32(((zMax - zMin) / Grid_size_init) + 1);

		double i_grid_size = (xMax - xMin) / x_grid;
		double j_grid_size = (yMax - yMin) / y_grid;
		double k_grid_size = (zMax - zMin) / z_grid;


		// 3D grid :  Fill the 3D Grid using the delocalization
		// ------------------------------------------------------
		int N_display = Convert.ToInt32(pos_length / Disp_step);
		int Delta = Convert.ToInt32(2 * Grid_delo / Grid_size_init);
		double den = 2 * Grid_delo * Grid_delo;

		Grid_st[,,] Grid = new Grid_st[x_grid + 1, y_grid + 1, z_grid + 1];
		Grid_st[,,] Grid_rnd = new Grid_st[x_grid + 1, y_grid + 1, z_grid + 1];
		int[,] ato_grid = new int[pos_length, 3];

		double x_cal;
		double y_cal;
		double z_cal;
		double dist_Grid_sqrt;
		double W;

		int i; int ii;
		int j; int jj;
		int k; int kk;

		Console.WriteLine("		");
		Console.WriteLine("Grid computation progress :");

		for (ite = 0; ite < pos_length; ite++)
		{

			// Position of an impact on the grid
			i = (int)(pos[ite, 0] / i_grid_size);
			j = (int)(pos[ite, 1] / j_grid_size);
			k = (int)(pos[ite, 2] / k_grid_size);

			// Associate to an impact its position on the grid
			ato_grid[ite, 0] = i;
			ato_grid[ite, 1] = j;
			ato_grid[ite, 2] = k;

			// Loop on the surrounding bloc define by the delocalization to fill the grid
			for (ii = (i - Delta); ii < (i + Delta + 1); ii++)
			{
				for (jj = (j - Delta); jj < (j + Delta + 1); jj++)
				{
					for (kk = (k - Delta); kk < (k + Delta + 1); kk++)
					{
						if (ii >= 0 && jj >= 0 && kk >= 0 && ii <= (x_grid) && jj <= (y_grid) && kk <= (z_grid))
						{
							x_cal = pos[ite, 0] - (ii * i_grid_size + i_grid_size / 2);
							y_cal = pos[ite, 1] - (jj * j_grid_size + j_grid_size / 2);
							z_cal = pos[ite, 2] - (kk * k_grid_size + k_grid_size / 2);
							dist_Grid_sqrt = x_cal * x_cal + y_cal * y_cal + z_cal * z_cal;
							W = Math.Exp(-1 * dist_Grid_sqrt / den);

							if (name[ite] == Name_Id)
							{
								Grid[ii, jj, kk].N_int = Grid[ii, jj, kk].N_int + W;
							}
							if (mass[ite] != 255)
							{
								Grid[ii, jj, kk].N_com = Grid[ii, jj, kk].N_com + W;
							}

							if (name_rnd[ite] == Name_Id)
							{
								Grid_rnd[ii, jj, kk].N_int = Grid_rnd[ii, jj, kk].N_int + W;
							}
							if (mass_rnd[ite] != 255)
							{
								Grid_rnd[ii, jj, kk].N_com = Grid_rnd[ii, jj, kk].N_com + W;
							}

						}
					}
				}
			}

			// Display the evolution of the computation
			if (ite % N_display == N_display - 1)
			{
				Console.WriteLine("		{0} / {1} impacts ", ite, pos_length);
			}

		};


		// Save the grid
		// --------------
		/*
		string path = @"d:\\Cameca\\Grid_density.txt";
		StreamWriter Write_Grid_composition = new StreamWriter(path);
		for (ii = 0; ii <x_grid ; ii++)
		{
			for (jj = 0; jj <y_grid ; jj++)
			{
				for (kk = 0; kk <z_grid ; kk++)
				{
					Write_Grid_composition.WriteLine( (ii*i_grid_size + i_grid_size/2) + " " + (jj*j_grid_size + j_grid_size/2)  + " " + (kk*k_grid_size + k_grid_size/2)  + " " + Grid[ii,jj,kk].N_int + " " + Grid[ii,jj,kk].N_com + " " + Grid_rnd[ii,jj,kk].N_int + " " + Grid_rnd[ii,jj,kk].N_com) ;
				}
			}
		}
		Write_Grid_composition.Close();
		*/


		// Trilinear interpolation
		// ------------------------

		Console.WriteLine("		");
		Console.WriteLine("Trilinear interpolation :");
		int[,] Id_Shift = new int[pos_length, 3];               // Shift vector
		double[,] dpos = new double[pos_length, 3];         // X, Y and Z shift proportion
		double[,] Cf = new double[pos_length, 2];

		int ll;

		//Parallel.For(0,pos_length, ll =>
		for (ll = 0; ll < (pos_length); ll++)
		{

			// Local neighbour : Define the vector
			Id_Shift[ll, 0] = (int)((pos[ll, 0] - ato_grid[ll, 0] * i_grid_size - i_grid_size / 2) / (Math.Abs(pos[ll, 0] - ato_grid[ll, 0] * i_grid_size - i_grid_size / 2)));
			Id_Shift[ll, 1] = (int)((pos[ll, 1] - ato_grid[ll, 1] * j_grid_size - j_grid_size / 2) / (Math.Abs(pos[ll, 1] - ato_grid[ll, 1] * j_grid_size - j_grid_size / 2)));
			Id_Shift[ll, 2] = (int)((pos[ll, 2] - ato_grid[ll, 2] * k_grid_size - k_grid_size / 2) / (Math.Abs(pos[ll, 2] - ato_grid[ll, 2] * k_grid_size - k_grid_size / 2)));

			// Id of the min bloc position
			int idx;
			int idy;
			int idz;

			double C000; double C100; double C001; double C101;
			double C010; double C110; double C011; double C111;
			double C00; double C01; double C10; double C11;
			double C0; double C1;


			idx = GetMin(ato_grid[ll, 0] + Id_Shift[ll, 0], ato_grid[ll, 0]);
			idy = GetMin(ato_grid[ll, 1] + Id_Shift[ll, 1], ato_grid[ll, 1]);
			idz = GetMin(ato_grid[ll, 2] + Id_Shift[ll, 2], ato_grid[ll, 2]);

			// X, Y and Z shift proportion
			dpos[ll, 0] = (pos[ll, 0] - idx * i_grid_size - i_grid_size / 2) / i_grid_size;
			dpos[ll, 1] = (pos[ll, 1] - idy * j_grid_size - j_grid_size / 2) / j_grid_size;
			dpos[ll, 2] = (pos[ll, 2] - idz * k_grid_size - k_grid_size / 2) / k_grid_size;

			// Boundary conditions
			if (idx >= 0 && idy >= 0 && idz >= 0 && idx < (x_grid - 1) && idy < (y_grid - 1) && idz < (z_grid - 1))
			{
				// Experimental data
				C000 = Grid[idx, idy, idz].N_int * 100 / (Grid[idx, idy, idz].N_com);
				C100 = Grid[idx + 1, idy, idz].N_int * 100 / (Grid[idx + 1, idy, idz].N_com);
				C001 = Grid[idx, idy, idz + 1].N_int * 100 / (Grid[idx, idy, idz + 1].N_com);
				C101 = Grid[idx + 1, idy, idz + 1].N_int * 100 / (Grid[idx + 1, idy, idz + 1].N_com);
				C010 = Grid[idx, idy + 1, idz].N_int * 100 / (Grid[idx, idy + 1, idz].N_com);
				C110 = Grid[idx + 1, idy + 1, idz].N_int * 100 / (Grid[idx + 1, idy + 1, idz].N_com);
				C011 = Grid[idx, idy + 1, idz + 1].N_int * 100 / (Grid[idx, idy + 1, idz + 1].N_com);
				C111 = Grid[idx + 1, idy + 1, idz + 1].N_int * 100 / (Grid[idx + 1, idy + 1, idz + 1].N_com);

				C00 = C000 * (1 - dpos[ll, 0]) + dpos[ll, 0] * C100;
				C01 = C001 * (1 - dpos[ll, 0]) + dpos[ll, 0] * C101;
				C10 = C010 * (1 - dpos[ll, 0]) + dpos[ll, 0] * C110;
				C11 = C011 * (1 - dpos[ll, 0]) + dpos[ll, 0] * C111;

				C0 = C00 * (1 - dpos[ll, 1]) + dpos[ll, 1] * C10;
				C1 = C01 * (1 - dpos[ll, 1]) + dpos[ll, 1] * C11;

				Cf[ll, 0] = C0 * (1 - dpos[ll, 2]) + dpos[ll, 2] * C1;

				// Random data
				C000 = Grid_rnd[idx, idy, idz].N_int * 100 / (Grid_rnd[idx, idy, idz].N_com);
				C100 = Grid_rnd[idx + 1, idy, idz].N_int * 100 / (Grid_rnd[idx + 1, idy, idz].N_com);
				C001 = Grid_rnd[idx, idy, idz + 1].N_int * 100 / (Grid_rnd[idx, idy, idz + 1].N_com);
				C101 = Grid_rnd[idx + 1, idy, idz + 1].N_int * 100 / (Grid_rnd[idx + 1, idy, idz + 1].N_com);
				C010 = Grid_rnd[idx, idy + 1, idz].N_int * 100 / (Grid_rnd[idx, idy + 1, idz].N_com);
				C110 = Grid_rnd[idx + 1, idy + 1, idz].N_int * 100 / (Grid_rnd[idx + 1, idy + 1, idz].N_com);
				C011 = Grid_rnd[idx, idy + 1, idz + 1].N_int * 100 / (Grid_rnd[idx, idy + 1, idz + 1].N_com);
				C111 = Grid_rnd[idx + 1, idy + 1, idz + 1].N_int * 100 / (Grid_rnd[idx + 1, idy + 1, idz + 1].N_com);

				C00 = C000 * (1 - dpos[ll, 0]) + dpos[ll, 0] * C100;
				C01 = C001 * (1 - dpos[ll, 0]) + dpos[ll, 0] * C101;
				C10 = C010 * (1 - dpos[ll, 0]) + dpos[ll, 0] * C110;
				C11 = C011 * (1 - dpos[ll, 0]) + dpos[ll, 0] * C111;

				C0 = C00 * (1 - dpos[ll, 1]) + dpos[ll, 1] * C10;
				C1 = C01 * (1 - dpos[ll, 1]) + dpos[ll, 1] * C11;

				Cf[ll, 1] = C0 * (1 - dpos[ll, 2]) + dpos[ll, 2] * C1;
			}
			else
			{
				Cf[ll, 0] = Grid[ato_grid[ll, 0], ato_grid[ll, 1], ato_grid[ll, 2]].N_int * 100 / (Grid[ato_grid[ll, 0], ato_grid[ll, 1], ato_grid[ll, 2]].N_com);
				Cf[ll, 1] = Grid_rnd[ato_grid[ll, 0], ato_grid[ll, 1], ato_grid[ll, 2]].N_int * 100 / (Grid_rnd[ato_grid[ll, 0], ato_grid[ll, 1], ato_grid[ll, 2]].N_com);
			}

			// Display the computation evolution if the computation is not parallel
			if (ll % N_display == N_display - 1)
			{
				Console.WriteLine("		{0} / {1} impacts ", ll, pos_length);
			}


		}
		//});

		// Concentration histogramm : Exp
		// ------------------------------------
		Console.WriteLine("		");
		Console.WriteLine("Experimental concentration distribution ");
		int[] X_hist = new int[Convert.ToInt32(100 / X_step)];
		for (ite = 0; ite < pos_length; ite++)
		{
			if (name[ite] != 255 && Cf[ite, 0] > 0) // && (product[ite]/sum[ite]) < 100)
			{
				int id_hist = Convert.ToInt32(Math.Truncate((Cf[ite, 0]) / X_step));
				X_hist[id_hist] = X_hist[id_hist] + 1;
			}
		}

		// Concentration histogramm : random
		// ----------------------------------------
		Console.WriteLine("Random concentration distribution ");
		int[] X_hist_rnd = new int[Convert.ToInt32(100 / X_step)];
		for (ite = 0; ite < pos_length; ite++)
		{
			if (name_rnd[ite] != 255 && Cf[ite, 1] > 0)// && (product[ite]/sum[ite]) < 100)
			{
				int id_hist_rnd = Convert.ToInt32(Math.Truncate((Cf[ite, 1]) / X_step));
				X_hist_rnd[id_hist_rnd] = X_hist_rnd[id_hist_rnd] + 1;
			}
		}


		// Filtering : Even the noive
		// ---------------------------
		Console.WriteLine("Filtering ");
		int[] Filter = new int[pos_length];
		for (ll = 0; ll < (pos_length); ll++)
		{
			if (Cf[ll, 0] > X_threshold)
			{
				Filter[ll] = 1;
			}
		}


		// Save in ato file : Gen 3
		// ------------------------
		Console.WriteLine("Save results in ato file ");

		string fileName = @"d:\\Cameca\\test.ato";
		UInt32 ideb1 = 0;
		UInt32 ideb2 = 3;
		try
		{
			using (BinaryWriter writer = new BinaryWriter(File.Open(fileName, FileMode.Create)))

			{
				writer.Write(ideb1);
				writer.Write(ideb2);
				for (ll = 0; ll < (pos_length); ll++)
				{
					writer.Write((float)(pos_init[ll].X * 10)); //x
					writer.Write((float)(pos_init[ll].Y * 10)); //y
					writer.Write((float)(pos_init[ll].Z * 10)); //z
					writer.Write((float)(mass[ll])); //m
					writer.Write((float)(Filter[ll])); //atom.id);//clusterid
					writer.Write((float)(1)); //nbPulse
					writer.Write((float)(0)); //vdc
					writer.Write((float)(0)); //tof
					writer.Write((float)(0)); // + ((tDet * 1e2) / 2)))/3); //Px
					writer.Write((float)(0)); // + ((tDet * 1e2) / 2)))/3); //Py
					writer.Write((float)(0)); //ampli
					writer.Write((float)(0));
					writer.Write((float)(0));
					writer.Write((float)(0));

				}
			}
		}
		catch (IOException)
		{
			// TODO: Indicate failure to write to file
		}


		// Close Console
		// ----------------
		//MessageBox.Show("Computation done !! Press OK to close!");  
		ConsoleHelper.FreeConsole();

		// Display the concentration distribution in IVAS
		// --------------------------------------------------
		int N_max_rnd = X_hist_rnd.Max();
		int N_max = X_hist.Max();

		float[] xVals = new float[Convert.ToInt32(100 / X_step)];

		float[] exp_dist = new float[Convert.ToInt32(100 / X_step)];
		float[] rnd_dist = new float[Convert.ToInt32(100 / X_step)];
		for (i = 0; i < exp_dist.Length; i++)
		{
			xVals[i] = (float)(i * X_step + X_step / 2);
			exp_dist[i] = X_hist[i] * 100f / (float)N_max;
		}
		for (i = 0; i < exp_dist.Length; i++)
		{
			rnd_dist[i] = X_hist_rnd[i] * 100f / (float)N_max_rnd;
		}

		return new FrequencyDistributionComparisonData(
			xVals.Zip(exp_dist).Select(x => new Vector3(x.First, 0f, x.Second)).ToArray(),
			xVals.Zip(rnd_dist).Select(x => new Vector3(x.First, 0f, x.Second)).ToArray());
	}

	// Function Console
	public class ConsoleHelper
	{
		[DllImport("kernel32.dll")]
		public static extern IntPtr GetStdHandle(int nStdHandle);

		/// <summary>
		/// Allocates a new console for current process.
		/// </summary>
		[DllImport("kernel32.dll")]
		public static extern bool AllocConsole();

		/// <summary>
		/// Frees the console.
		/// </summary>
		[DllImport("kernel32.dll")]
		public static extern bool FreeConsole();

		public const int STD_OUTPUT_HANDLE = -11;
		public const int MY_CODE_PAGE = 437;
	}

	// Function Random
	static Random random = new Random();
	public static double GetRandomNumber(int minimum, int maximum)
	{
		return random.NextDouble() * (maximum - minimum) + minimum;
	}

	// Console properties
	public void vPrepareConsole()
	{
		IntPtr stdHandle = ConsoleHelper.GetStdHandle(ConsoleHelper.STD_OUTPUT_HANDLE);
		SafeFileHandle safeFileHandle = new SafeFileHandle(stdHandle, true);
		FileStream fileStream = new FileStream(safeFileHandle, FileAccess.Write);
		Encoding encoding = System.Text.Encoding.GetEncoding(ConsoleHelper.MY_CODE_PAGE);
		StreamWriter standardOutput = new StreamWriter(fileStream, encoding);
		standardOutput.AutoFlush = true;
		Console.SetOut(standardOutput);
	}

	// Grid structure
	public struct Grid_st
	{
		public double N_int;
		public double N_com;
	}

	//public struct X_gridst
	//{
	//	public double Exp;
	//	public double Rnd;
	//}

	// Return the greater value of two numbers 
	public static int GetMax(int first, int second)
	{
		if (first > second)
		{
			return first;
		}
		else
		{
			return second;
		}
	}

	// Return the lower value of two numbers 
	public static int GetMin(int first, int second)
	{
		if (first > second)
		{
			return second;
		}
		else
		{
			return first;
		}
	}
}
