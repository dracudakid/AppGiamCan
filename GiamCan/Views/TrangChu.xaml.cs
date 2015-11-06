﻿using GiamCan.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace GiamCan.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TrangChu : Page
    {
        string path;
        SQLite.Net.SQLiteConnection conn;
        NguoiDung nguoidung;
        MucTieu muctieuhientai;
        ThongKeNgay thongkengay;
        public TrangChu()
        {
            this.InitializeComponent();
            path = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "giamcandb.sqlite");
            conn = new SQLite.Net.SQLiteConnection(new SQLite.Net.Platform.WinRT.SQLitePlatformWinRT(), path);
        }
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            // lay nguoidung tu trang dang nhap vao
            nguoidung = (NguoiDung)e.Parameter;

            // kiem tra ngay hien tai da co trong db chua, chua co thi them vao
            muctieuhientai = conn.Table<MucTieu>().Where(r => r.TenDangNhap == nguoidung.TenDangNhap && (r.TrangThai == "Đã bắt đầu" || r.TrangThai == "Chưa bắt đầu")).FirstOrDefault();

            // neu muctieuhientai == null ---> tuc nguoidung chua co muc tieu nao hien tai
            if (muctieuhientai == null)
            {
                MessageDialog msDialog = new MessageDialog("Bạn chưa bắt đầu một mục tiêu nào!");
                msDialog.Commands.Add(new UICommand("Bắt đầu ngay"));
                msDialog.Commands.Add(new UICommand("Để sau"));
                var result = await msDialog.ShowAsync();
                if (result.Label != "Để sau")
                {
                    denTaoMucTieuMoiPage();
                }
                return;
            }

            string today = DateTime.Today.ToString("dd/MM/yyyy");
            // neu nguoi dung chua bat dau muc tieu hien tai
            if (muctieuhientai.ThoiGianBatDau == null || muctieuhientai.TrangThai == "Chưa bắt đầu")
            {
                MessageDialog msDialog = new MessageDialog("Bạn vẫn chưa bắt đầu tập luyện!\n");
                msDialog.Commands.Add(new UICommand("Ok, đến trang tập luyện"));
                msDialog.Commands.Add(new UICommand("Để sau"));
                var result = await msDialog.ShowAsync();
                if (result.Label.Equals("Để sau"))
                {
                    return;
                }
                else
                {
                    // dat thoi gian bat dau la ngay hom nay
                    muctieuhientai.ThoiGianBatDau = today;
                    muctieuhientai.TrangThai = "Đã bắt đầu";
                    conn.Update(muctieuhientai);
                    Frame.Navigate(typeof(DanhSachBaiTap));
                }

            }
            // kiem tra thoigianketthuc cua muc tieu

            //DateTime ngayketthuc = DateTime.Parse(muctieuhientai.ThoiGianBatDau).AddDays((int)muctieuhientai.SoNgay); <--- nho dung ParseExact de theo chuan dd/MM/yyyy
            DateTime ngayketthuc = DateTime.ParseExact(muctieuhientai.ThoiGianBatDau, "dd/MM/yyyy", new CultureInfo("vi-vn"));

            // neu da vuot qua so ngay
            if (DateTime.Today > ngayketthuc)
            {
                muctieuhientai.TrangThai = "Hoàn thành";
                conn.Update(muctieuhientai);
                MessageDialog msDialog = new MessageDialog("Chúc mừng, bạn đã tập hết số ngày của mục tiêu đề ra\nHãy xem lại quá trình luyện tập của bạn!");
                msDialog.Commands.Add(new UICommand("Xem thống kê"));
                msDialog.Commands.Add(new UICommand("Mục tiêu mới"));
                var result = await msDialog.ShowAsync();
                if (result.Label.Equals("Mục tiêu mới"))
                {
                    // chuyển tới trang mục tiêu mới
                    denTaoMucTieuMoiPage();
                }
                else
                {
                    // chuyển đến trang thống kê
                    Frame.Navigate(typeof(ThongKePage), muctieuhientai);
                }
            }
            // neu van con trong thoi han cua muc tieu
            else
            {
                thongkengay = conn.Table<ThongKeNgay>().Where(r => r.IdMucTieu == muctieuhientai.IdMucTieu && r.Ngay == today).FirstOrDefault();
                if (thongkengay == null)
                {
                    thongkengay = new ThongKeNgay();
                    thongkengay.IdMucTieu = muctieuhientai.IdMucTieu;
                    thongkengay.Ngay = today;
                    conn.Insert(thongkengay);
                }
            }

        }

        private void nhacnhoButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(DatNhacNho), nguoidung);
        }

        private void muctieumoiButton_Click(object sender, RoutedEventArgs e)
        {
            denTaoMucTieuMoiPage();
        }

        private void thongtinButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ThongTinCaNhan), nguoidung);
        }

        private void baitapButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(DanhSachBaiTap));
        }

        private void thongkeButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ThongKePage), muctieuhientai);
        }

        private void chedoanButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MonAnPage), thongkengay);
        }

        private async void denTaoMucTieuMoiPage()
        {
            // kiem tra muctieuhientai da hoan thanh chua, neu chua thi se hien len thong bao
            if (muctieuhientai != null && muctieuhientai.TrangThai != "Hoàn thành")
            {
                MessageDialog msDialog = new MessageDialog("TẠO MỚI MỤC TIÊU\nBạn sẽ hủy mục tiêu hiện tại?");

                msDialog.Commands.Add(new UICommand("Đồng ý") { Id = 0 });
                msDialog.Commands.Add(new UICommand("Trở về") { Id = 1 });
                msDialog.DefaultCommandIndex = 1;
                var result = await msDialog.ShowAsync();
                if (result.Label == "Đồng ý")
                {
                    muctieuhientai.TrangThai = "Hủy";
                    conn.Update(muctieuhientai);
                }
                else
                {
                    return;
                }
            }
            Frame.Navigate(typeof(TaoMoiMucTieu), nguoidung);
        }
    }
}
