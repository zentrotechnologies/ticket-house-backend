using MODEL.Response;
using QRCoder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BAL.Services
{
    public interface IQRCodeService
    {
        byte[] GenerateQRCode(string data);
        string GenerateQRCodeBase64(string data);
        Task<string> GenerateBookingQRCodeAsync(int bookingId, BookingDetailsResponse bookingDetails);
    }

    public class QRCodeService : IQRCodeService
    {
        public byte[] GenerateQRCode(string data)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);

            // ✅ THIS is Linux safe
            var pngQrCode = new PngByteQRCode(qrCodeData);

            byte[] qrCodeBytes = pngQrCode.GetGraphic(20);

            return qrCodeBytes;
        }

        public string GenerateQRCodeBase64(string data)
        {
            byte[] qrCodeBytes = GenerateQRCode(data);
            return Convert.ToBase64String(qrCodeBytes);
        }

        public async Task<string> GenerateBookingQRCodeAsync(int bookingId, BookingDetailsResponse bookingDetails)
        {
            try
            {
                var qrData = new
                {
                    BookingId = bookingId,
                    BookingCode = bookingDetails.booking_code,
                    EventName = bookingDetails.event_name,
                    EventDate = bookingDetails.event_date.ToString("yyyy-MM-dd"),
                    EventTime = $"{bookingDetails.start_time} - {bookingDetails.end_time}",
                    Location = bookingDetails.location,
                    CustomerName = $"{bookingDetails.first_name} {bookingDetails.last_name}",
                    CustomerEmail = bookingDetails.email,
                    TotalAmount = bookingDetails.total_amount,
                    FinalAmount = bookingDetails.final_amount,
                    Status = bookingDetails.status,
                    BookingDate = bookingDetails.created_on.ToString("yyyy-MM-dd HH:mm:ss"),
                    Seats = bookingDetails.BookingSeats.Select(bs => new
                    {
                        SeatType = bs.seat_name,
                        Quantity = bs.quantity,
                        Price = bs.price_per_seat,
                        Subtotal = bs.subtotal
                    }).ToList()
                };

                string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(qrData);

                return GenerateQRCodeBase64(jsonData);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating QR code: {ex.Message}");
            }
        }
    }
}