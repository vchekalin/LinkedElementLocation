#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion

namespace LinkedElementLocation
{
  [Transaction( TransactionMode.Manual )]
  public class Command : IExternalCommand
  {
  #region Formatting
  const double _inch_to_mm = 25.4;
  const double _foot_to_mm = 12 * _inch_to_mm;

  /// <summary>
  /// Возвращает строку для числа с плавающей точкой
  /// отформатированную до двух знаков после запятой
  /// </summary>
  public static string RealString( double a )
  {
    return a.ToString( "0.##" );
  }

  /// <summary>
  /// Возвращает строку для трехмерной точки XYZ
  /// или вектора с координатами этой точки
  /// и отформатированную до двух знаков после запятой
  /// </summary>
  public static string PointString( XYZ p )
  {
    return string.Format( "({0};{1};{2})",
      RealString( p.X ),
      RealString( p.Y ),
      RealString( p.Z ) );
  }

  /// <summary>
  /// Возвращает строку для трехмерной точки XYZ
  /// или вектора с координатами этой точки
  /// преобразованную из футов в миллиметры
  /// и отформатированную до двух знаков после запятой
  /// </summary>
  public static string PointStringMm( XYZ p )
  {
    return string.Format( "({0};{1};{2})",
      RealString( p.X * _foot_to_mm ),
      RealString( p.Y * _foot_to_mm ),
      RealString( p.Z * _foot_to_mm ) );
  }
  #endregion // Formatting

    #region LinkSelectionFilter
    public class LinkSelectionFilter : ISelectionFilter
    {
      public bool AllowElement( Element e )
      {
        return e is RevitLinkInstance;
      }

      public bool AllowReference( Reference r, XYZ p )
      {
        throw new NotImplementedException();
      }
    }
    #endregion // LinkSelectionFilter

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;
      Selection sel = uidoc.Selection;

      Reference r = sel.PickObject(
        ObjectType.Element,
        new LinkSelectionFilter(),
        "Выберите связанный файл" );

      RevitLinkInstance rvtlink = doc.GetElement( r )
        as RevitLinkInstance;

      if( rvtlink == null )
      {
        return Result.Failed;
      }

      // в примере будем рассматривать только 
      // красную и синюю стены

      var walls = new FilteredElementCollector(
          rvtlink.GetLinkDocument() )
        .OfClass( typeof( Wall ) )
        .Where(c => c.Id.IntegerValue == 197525
          || c.Id.IntegerValue == 197622 );

      // Определяем смещение месторасположения связанного файла в исходном

      Transform t = rvtlink.GetTotalTransform();

      foreach( Wall wall in walls )
      {
          // Определим координаты; Но эти координаты
          // относятся к связанному файла, т.е. к проекту А.


        LocationCurve curve = wall.Location
          as LocationCurve;

        XYZ p = curve.Curve.GetEndPoint( 0 );
        XYZ q = curve.Curve.GetEndPoint( 1 );

        string title = "{0} стена";

        string msg = "{0} стена располагается в точке {1} "
          + "исходного проекта  и в точке {2} в текущем проекте"          ;

        bool red = (wall.Id.IntegerValue == 197525);

        TaskDialog.Show(
          string.Format( title, red ? "Красная" : "Синяя" ),
          string.Format( msg,
            red ? "Красная" : "Синяя",
            PointStringMm( q ),
            PointStringMm( t.OfPoint( q ) ) ) );
      }
      return Result.Succeeded;
    }
  }
}
