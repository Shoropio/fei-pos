using System;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.IO;

namespace FeiPos.Infrastructure.Security
{
    public class XmlDigitalSigner
    {
        public static string SignXades(string xmlContent, string certPath, string pin)
        {
            if (!File.Exists(certPath))
                throw new FileNotFoundException("Certificado digital no encontrado en la ruta especificada.");

            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.LoadXml(xmlContent);

            var cert = new X509Certificate2(certPath, pin, X509KeyStorageFlags.Exportable);
            
            var signedXml = new SignedXml(doc)
            {
                SigningKey = cert.GetRSAPrivateKey()
            };

            // 1. Referencia al documento completo
            var reference = new Reference { Uri = "" };
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            signedXml.AddReference(reference);

            // 2. Información del Certificado (KeyInfo)
            var keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoX509Data(cert));
            signedXml.KeyInfo = keyInfo;

            // 3. Cálculo de la firma
            signedXml.ComputeSignature();

            // 4. Inserción de la firma en el XML
            var xmlSignature = signedXml.GetXml();
            doc.DocumentElement?.AppendChild(doc.ImportNode(xmlSignature, true));

            return doc.OuterXml;
        }
    }
}
