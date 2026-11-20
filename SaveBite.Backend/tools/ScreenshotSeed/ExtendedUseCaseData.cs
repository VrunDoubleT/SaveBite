using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;

internal static class ExtendedUseCaseData
{
 static Guid Id(int kind,int i)=>Guid.Parse($"e1000000-0000-0000-{kind:0000}-{i:000000000000}");
 public static async Task ApplyAsync(AppDbContext db)
 {
  if(db.Database.GetDbConnection().Database!="savebite_uc050")throw new Exception("Clone database required");
  var now=DateTime.UtcNow;var customer=await db.Users.SingleAsync(u=>u.Email=="customer@gmail.com");var owner=await db.Users.SingleAsync(u=>u.Email=="owner@gmail.com");var staff=await db.Users.SingleAsync(u=>u.Email=="customer.locked@gmail.com");staff.CustomerStatus=CustomerStatus.Active;
  var shop=await db.Shops.FirstAsync(s=>s.OwnerUserId==owner.Id);shop.OpeningTime=new TimeOnly(0,0);shop.ClosingTime=new TimeOnly(0,0);var shops=await db.Shops.Where(s=>s.Status==ShopStatus.Active).Take(8).ToListAsync();
  for(int i=1;i<=5;i++){if(!await db.Users.AnyAsync(u=>u.Id==Id(1,i))){var u=new User();db.Entry(u).CurrentValues.SetValues(customer);u.Id=Id(1,i);u.Email=$"documentation.applicant{i}@savebite.test";u.Phone=$"090110000{i}";u.FullName=new[]{"Jordan Parker","Casey Brooks","Morgan Blake","Taylor Reed","Avery Stone"}[i-1];u.Role=UserRole.User;u.CreatedAt=now;u.UpdatedAt=now;u.PasswordHash=BCrypt.Net.BCrypt.HashPassword("SaveBite@123");db.Add(u);}}
  await db.SaveChangesAsync();
  for(int i=2;i<=3;i++){if(!await db.Set<ShopApplication>().AnyAsync(a=>a.Id==Id(2,i))){var a=await db.Set<ShopApplication>().AsNoTracking().FirstAsync();var n=new ShopApplication();db.Entry(n).CurrentValues.SetValues(a);n.Id=Id(2,i);n.ApplicantUserId=Id(1,i);n.Name=i==2?"Sunrise Community Kitchen":"Harvest Corner Bakery";n.Description="Freshly prepared meals and baked goods, available for local collection.";n.Status=i==2?ShopApplicationStatus.NeedsRevision:ShopApplicationStatus.Pending;n.CreatedAt=now;n.UpdatedAt=now;n.BusinessLicenseNo=$"100000000{i}";db.Add(n);foreach(var doc in await db.Set<ShopApplicationDocument>().AsNoTracking().Where(d=>d.ApplicationId==a.Id&&d.IsCurrent).ToListAsync()){var d=new ShopApplicationDocument();db.Entry(d).CurrentValues.SetValues(doc);d.Id=Guid.NewGuid();d.ApplicationId=n.Id;d.UploadedBy=n.ApplicantUserId;d.OriginalFileName="business-document.png";db.Add(d);}db.Add(new ShopApplicationReviewLog{Id=Guid.NewGuid(),ApplicationId=n.Id,ToStatus=n.Status,RevisionNumber=1,Note=i==2?"Please update the business description and resubmit your application.":"Application submitted for review.",CreatedAt=now});}}
  if(!await db.Set<ShopStaff>().AnyAsync(x=>x.ShopId==shop.Id&&x.UserId==staff.Id))db.Add(new ShopStaff{Id=Id(3,1),ShopId=shop.Id,UserId=staff.Id,DisplayName="Jamie Wilson",Nickname="Evening team",Status=ShopStaffStatus.Active,JoinedAt=now.AddDays(-20)});
  foreach(var st in await db.Set<ShopStaff>().ToListAsync()){st.DisplayName="Jamie Wilson";st.Nickname="Evening team";}
  for(int i=0;i<Math.Min(shops.Count,6);i++){var sh=shops[i];var iid=Id(4,i+1);if(!await db.Set<StaffInvitation>().AnyAsync(x=>x.Id==iid))db.Add(new StaffInvitation{Id=iid,ShopId=sh.Id,InvitedUserId=customer.Id,InvitedBy=sh.OwnerUserId,Status=i<3?"Pending":i==3?"Accepted":"Declined",InvitedAt=now.AddMinutes(-i*5)});}
  for(int i=1;i<=5;i++){var iid=Id(4,10+i);if(!await db.Set<StaffInvitation>().AnyAsync(x=>x.Id==iid))db.Add(new StaffInvitation{Id=iid,ShopId=shop.Id,InvitedUserId=i<=3?Id(1,i):customer.Id,InvitedBy=owner.Id,Status=i<=3?"Pending":"Declined",InvitedAt=now.AddMinutes(-i*3)});}
  foreach(var doc in await db.Set<ShopApplicationDocument>().ToListAsync())doc.OriginalFileName=doc.DocumentType.ToString()+".pdf";
  foreach(var product in await db.Products.Where(p=>p.ShopId==shop.Id).ToListAsync()){
   if(await db.Set<ProductVariant>().AnyAsync(v=>v.ProductId==product.Id))continue;
   var a=new ProductAttribute{Id=Guid.NewGuid(),ProductId=product.Id,Name="Portion",SortOrder=0};
   var value=new ProductAttributeValue{Id=Guid.NewGuid(),AttributeId=a.Id,Value="Regular",SortOrder=0};
   var v=new ProductVariant{Id=Guid.NewGuid(),ProductId=product.Id,Sku="DEMO-"+product.Id.ToString()[^8..],OriginalPrice=50000,DealPrice=30000,IsActive=true,CreatedAt=now};
   db.AddRange(a,value,v);db.Add(new ProductVariantValue{VariantId=v.Id,AttributeValueId=value.Id});
  }
  var template=await db.Orders.AsNoTracking().FirstAsync(o=>o.ShopId==shop.Id&&o.Status==OrderStatus.Completed);var items=await db.Set<OrderItem>().AsNoTracking().Where(x=>x.OrderId==template.Id).ToListAsync();var pay=await db.Set<Payment>().AsNoTracking().FirstAsync(x=>x.OrderId==template.Id);
  for(int i=1;i<=12;i++){
   if(await db.Orders.AnyAsync(o=>o.Id==Id(5,i))){
    if(i<=3){var existing=await db.Orders.SingleAsync(o=>o.Id==Id(5,i));if(existing.Status==OrderStatus.Pending){existing.CreatedAt=now;existing.UpdatedAt=now;foreach(var existingPayment in await db.Set<Payment>().Where(p=>p.OrderId==existing.Id).ToListAsync())existingPayment.PaidAt=now;}}
    continue;
   }
   var o=new Order();db.Entry(o).CurrentValues.SetValues(template);o.Id=Id(5,i);o.CustomerId=customer.Id;o.OrderCode=$"710{i:000}";o.CreatedAt=now.AddSeconds(-i);o.UpdatedAt=now;o.Status=i<=3?OrderStatus.Pending:i<=5?OrderStatus.Confirmed:i<=8?OrderStatus.ReadyForPickup:OrderStatus.Cancelled;o.CompletedAt=null;o.CancelledAt=i>=9?now:null;o.CancelReason=i>=9?"Customer requested cancellation before pickup.":null;o.SoftDeadlineAt=now.AddHours(3);o.HardDeadlineAt=now.AddHours(4);o.ConfirmedAt=i>3?now:null;o.ReadyAt=i>5&&i<9?now:null;o.InventoryReservationActive=true;o.IsPlatformFeeExempt=true;o.CheckoutRequestId=null;o.CheckoutQuoteId=null;o.ConfirmationExpirationJobId=null;o.PaymentExpirationJobId=null;o.ResaleDecisionDeadlineAt=i>=9?now.AddHours(4):null;
   if(i==8){o.PaymentMethod=PaymentMethod.PayAtShop;o.DepositAmount=10000;o.RemainingAmount=o.TotalAmount-10000;o.RequiredOnlineAmount=10000;}
   db.Add(o);
   for(int j=0;j<items.Count;j++){var it=new OrderItem();db.Entry(it).CurrentValues.SetValues(items[j]);it.Id=Id(6,i*10+j);it.OrderId=o.Id;it.ResaleDecisionStatus=i>=9?ResaleDecisionStatus.Pending:ResaleDecisionStatus.NotRequired;it.ResaleQuantity=it.UnsellableQuantity=0;it.ResaleDecidedAt=null;it.ResaleDecidedBy=null;db.Add(it);}
   var p=new Payment();db.Entry(p).CurrentValues.SetValues(pay);p.Id=Id(7,i);p.OrderId=o.Id;p.CreatedAt=now;p.UpdatedAt=now;p.PaidAt=now;p.GatewayTransactionId="DEMO-EXT-"+i;if(i==8){p.PaymentType=PaymentType.Deposit;p.Amount=p.CollectedAmount=10000;}db.Add(p);
  }
  var rule=await db.Set<RecoveryRequirementRule>().FirstAsync();
  for(int i=1;i<=3;i++)if(!await db.Set<NoShowPenalty>().AnyAsync(x=>x.Id==Id(8,i))){var o=new Order();db.Entry(o).CurrentValues.SetValues(template);o.Id=Id(5,20+i);o.CustomerId=customer.Id;o.OrderCode=$"720{i:000}";o.Status=OrderStatus.NoShow;o.NoShow=true;o.CreatedAt=now.AddDays(-i*5);o.UpdatedAt=now;o.CheckoutRequestId=null;o.CheckoutQuoteId=null;db.Add(o);db.Add(new NoShowPenalty{Id=Id(8,i),UserId=customer.Id,OrderId=o.Id,RecoveryRuleId=rule.Id,PenaltyYear=now.Year,PenaltyMonth=now.Month,OccurrenceNumber=i,ScoreDeducted=5,RequiredOnlineOrders=5,RequiredOnlineOrderValue=250000,CompletedOnlineOrders=i==1?2:5,CompletedOnlineOrderValue=i==1?100000:250000,Status=i==1?NoShowPenaltyStatus.Recovering:NoShowPenaltyStatus.Recovered,CreatedAt=now.AddDays(-i*5),RecoveredAt=i==1?null:now.AddDays(-1)});}
  await db.SaveChangesAsync();Console.WriteLine($"Extended demo ready: shop={shop.Id}, staff={staff.Email}, products={await db.Products.CountAsync(p=>p.ShopId==shop.Id)}");
 }
}
