// Test script to check JWT token
const token = localStorage.getItem('authToken');

if (!token) {
  console.log('❌ Token bulunamadı');
} else {
  console.log('✅ Token mevcut:', token.substring(0, 50) + '...');
  
  // Decode token
  try {
    const parts = token.split('.');
    if (parts.length === 3) {
      const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')));
      console.log('📦 Token payload:', payload);
      
      if (payload.exp) {
        const expDate = new Date(payload.exp * 1000);
        const now = new Date();
        const diffMs = expDate - now;
        const diffHours = diffMs / (1000 * 60 * 60);
        
        console.log('⏰ Token bitiş zamanı:', expDate.toLocaleString('tr-TR'));
        console.log('🕐 Şu anki zaman:', now.toLocaleString('tr-TR'));
        console.log('⏳ Kalan süre:', diffHours.toFixed(2), 'saat');
        
        if (diffMs > 0) {
          console.log('✅ Token hala geçerli');
        } else {
          console.log('❌ Token süresi dolmuş');
        }
      } else {
        console.log('⚠️ Token\'da exp claim yok!');
      }
    }
  } catch (e) {
    console.error('❌ Token decode hatası:', e);
  }
}
