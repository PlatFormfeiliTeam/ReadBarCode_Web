using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace ReadBarCode_Web
{
    class ConvertPDF
    {
        //public string pdfToPic(string sourcePdf, string outputPath)
        public static bool pdfToPic(string pdfInputPath, string outputPath, string imageName, int startPageNum, int endPageNum, ImageFormat imageFormat)
        {
            // NOTE:  This will only get the first image it finds per page.
            PdfReader pdf = new PdfReader(pdfInputPath);
            RandomAccessFileOrArray raf = new iTextSharp.text.pdf.RandomAccessFileOrArray(pdfInputPath);
            bool flag = true;
            try
            {
                for (int pageNumber = startPageNum; pageNumber <= endPageNum; pageNumber++)
                {
                    PdfDictionary pg = pdf.GetPageN(pageNumber);

                    // recursively search pages, forms and groups for images. 
                    PdfObject obj = FindImageInPDFDictionary(pg);
                    if (obj != null)
                    {

                        int XrefIndex = Convert.ToInt32(((PRIndirectReference)obj).Number.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        PdfObject pdfObj = pdf.GetPdfObject(XrefIndex);
                        PdfStream pdfStrem = (PdfStream)pdfObj;
                        byte[] bytes = PdfReader.GetStreamBytesRaw((PRStream)pdfStrem);
                        if ((bytes != null))
                        {
                            using (System.IO.MemoryStream memStream = new System.IO.MemoryStream(bytes))
                            {
                                memStream.Position = 0;
                                System.Drawing.Image img = System.Drawing.Image.FromStream(memStream);
                                // must save the file while stream is open.


                                if (!Directory.Exists(outputPath))
                                    Directory.CreateDirectory(outputPath);

                                //path = Path.Combine(outputPath, String.Format(@"{0}.jpg", pageNumber));
                                System.Drawing.Imaging.EncoderParameters parms = new System.Drawing.Imaging.EncoderParameters(1);
                                parms.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Compression, 0);
                                System.Drawing.Imaging.ImageCodecInfo jpegEncoder = GetImageEncoder("JPEG");
                                img.Save(outputPath + imageName + "." + imageFormat.ToString(), jpegEncoder, parms);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                flag = false;
            }
            finally
            {
                pdf.Close();
                raf.Close();
            }
            return flag;
        }

        private static PdfObject FindImageInPDFDictionary(PdfDictionary pg)
        {
            PdfDictionary res = (PdfDictionary)PdfReader.GetPdfObject(pg.Get(PdfName.RESOURCES));


            PdfDictionary xobj = (PdfDictionary)PdfReader.GetPdfObject(res.Get(PdfName.XOBJECT));
            if (xobj != null)
            {
                foreach (PdfName name in xobj.Keys)
                {
                    PdfObject obj = xobj.Get(name);
                    if (obj.IsIndirect())
                    {
                        PdfDictionary tg = (PdfDictionary)PdfReader.GetPdfObject(obj);
                        PdfName type = (PdfName)PdfReader.GetPdfObject(tg.Get(PdfName.SUBTYPE));

                        //image at the root of the pdf
                        if (PdfName.IMAGE.Equals(type))
                        {
                            return obj;
                        }// image inside a form
                        else if (PdfName.FORM.Equals(type))
                        {
                            return FindImageInPDFDictionary(tg);
                        } //image inside a group
                        else if (PdfName.GROUP.Equals(type))
                        {
                            return FindImageInPDFDictionary(tg);
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Obtain an image encoder suitable for a specific graphics format.
        /// </summary>
        /// <param name="imageType">One of: BMP, JPEG, GIF, TIFF, PNG.</param>
        /// <returns>An ImageCodecInfo corresponding with the type requested,
        /// or null if the type was not found.</returns>
        static ImageCodecInfo GetImageEncoder(string imageType)
        {
            imageType = imageType.ToUpperInvariant();
            foreach (ImageCodecInfo info in ImageCodecInfo.GetImageEncoders())
            {
                if (info.FormatDescription == imageType)
                {
                    return info;
                }
            }
            return null;
        }
    }
}
