import { useState, useEffect, useMemo } from "react";
import api from "./services/api";
import {
  ShoppingCart,
  LogIn,
  LogOut,
  AlertCircle,
  Search,
  Trash2,
  Plus,
  Minus,
  PackageCheck,
  ArrowUpDown,
  History,
  X,
  Truck,
  RotateCw,
  ServerOff,
} from "lucide-react";

interface Product {
  id: number;
  name: string;
  description: string;
  price: number;
  stockQuantity?: number;
  stock?: number;
  categoryName?: string;
}

interface OrderHistoryItem {
  id: number;
  createdAt: string;
  totalAmount: number;
}

export default function App() {
  const [products, setProducts] = useState<Product[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [fetchError, setFetchError] = useState(false);

  const [cart, setCart] = useState<{ product: Product; quantity: number }[]>(
    [],
  );
  const [coupon, setCoupon] = useState("");
  const [appliedDiscount, setAppliedDiscount] = useState<number>(0);
  const [couponStatus, setCouponStatus] = useState<{
    msg: string;
    isError: boolean;
  } | null>(null);

  // Arama, Kategori ve Sıralama
  const [selectedCategory, setSelectedCategory] = useState<string>("Tümü");
  const [searchQuery, setSearchQuery] = useState("");
  const [sortBy, setSortBy] = useState<"default" | "price-asc" | "price-desc">(
    "default",
  );

  // Auth & Siparişler
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [email, setEmail] = useState("arda@toker.local");
  const [password, setPassword] = useState("arda123");
  const [orderSuccess, setOrderSuccess] = useState(false);
  const [notification, setNotification] = useState<string | null>(null);

  // Sipariş Geçmişi
  const [showHistoryModal, setShowHistoryModal] = useState(false);
  const [orderHistory, setOrderHistory] = useState<OrderHistoryItem[]>([]);
  const [isLoadingHistory, setIsLoadingHistory] = useState(false);

  useEffect(() => {
    const token = localStorage.getItem("token");
    if (token) setIsLoggedIn(true);
    loadProducts();
  }, []);

  const loadProducts = async () => {
    setIsLoading(true);
    setFetchError(false);
    try {
      const res = await api.get("/products");
      const data = res.data.items || res.data;
      setProducts(data);
    } catch {
      setFetchError(true);
      showToast("Backend servisine ulaşılamadı. 5080 portunu kontrol edin.");
    } finally {
      setIsLoading(false);
    }
  };

  const fetchOrderHistory = async () => {
    if (!isLoggedIn) return;
    setIsLoadingHistory(true);
    setShowHistoryModal(true);
    try {
      const res = await api.get("/orders");
      setOrderHistory(res.data.items || res.data || []);
    } catch {
      showToast("Sipariş geçmişi yüklenirken hata oluştu.");
    } finally {
      setIsLoadingHistory(false);
    }
  };

  const showToast = (msg: string) => {
    setNotification(msg);
    setTimeout(() => setNotification(null), 3500);
  };

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const res = await api.post("/auth/login", { email, password });
      localStorage.setItem("token", res.data.token);
      setIsLoggedIn(true);
      showToast("Başarıyla giriş yapıldı.");
    } catch (err: any) {
      showToast(err.response?.data?.message || "Giriş bilgileri geçersiz.");
    }
  };

  const handleLogout = () => {
    localStorage.removeItem("token");
    setIsLoggedIn(false);
    setShowHistoryModal(false);
    showToast("Oturum kapatıldı.");
  };

  const fillDemoCredentials = () => {
    setEmail("arda@toker.local");
    setPassword("arda123");
    showToast("Test kullanıcı bilgileri forma aktarıldı.");
  };

  const getStock = (p: Product) => p.stockQuantity ?? p.stock ?? 0;

  const categories = useMemo(() => {
    const cats = Array.from(
      new Set(products.map((p) => p.categoryName || "Genel")),
    );
    return ["Tümü", ...cats];
  }, [products]);

  const processedProducts = useMemo(() => {
    let list = products.filter((p) => {
      const matchCat =
        selectedCategory === "Tümü" ||
        (p.categoryName || "Genel") === selectedCategory;
      const matchSearch =
        p.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        p.description.toLowerCase().includes(searchQuery.toLowerCase());
      return matchCat && matchSearch;
    });

    if (sortBy === "price-asc") {
      list = [...list].sort((a, b) => a.price - b.price);
    } else if (sortBy === "price-desc") {
      list = [...list].sort((a, b) => b.price - a.price);
    }

    return list;
  }, [products, selectedCategory, searchQuery, sortBy]);

  const addToCart = (product: Product) => {
    const stock = getStock(product);
    setCart((prev) => {
      const existing = prev.find((item) => item.product.id === product.id);
      if (existing) {
        if (existing.quantity >= stock) {
          showToast("Maksimum stok adedine ulaşıldı.");
          return prev;
        }
        return prev.map((item) =>
          item.product.id === product.id
            ? { ...item, quantity: item.quantity + 1 }
            : item,
        );
      }
      showToast(`${product.name} sepete eklendi.`);
      return [...prev, { product, quantity: 1 }];
    });
  };

  const updateQuantity = (productId: number, delta: number) => {
    setCart((prev) => {
      return prev
        .map((item) => {
          if (item.product.id === productId) {
            const newQty = item.quantity + delta;
            const maxStock = getStock(item.product);
            if (newQty > maxStock) {
              showToast("Stok limitini aşamazsınız.");
              return item;
            }
            return { ...item, quantity: newQty };
          }
          return item;
        })
        .filter((item) => item.quantity > 0);
    });
  };

  const removeFromCart = (productId: number) => {
    setCart((prev) => prev.filter((item) => item.product.id !== productId));
  };

  const applyCoupon = () => {
    const code = coupon.trim().toUpperCase();
    if (code === "INDIRIM10") {
      setAppliedDiscount(0.1);
      setCouponStatus({ msg: "%10 İndirim uygulandı", isError: false });
    } else {
      setAppliedDiscount(0);
      setCouponStatus({ msg: "Geçersiz kupon", isError: true });
    }
  };

  const subtotal = useMemo(() => {
    return cart.reduce(
      (acc, item) => acc + item.product.price * item.quantity,
      0,
    );
  }, [cart]);

  const total = useMemo(() => {
    return subtotal * (1 - appliedDiscount);
  }, [subtotal, appliedDiscount]);

  const freeShippingThreshold = 500;
  const progressToFreeShipping = Math.min(
    100,
    (subtotal / freeShippingThreshold) * 100,
  );

  const handleCheckout = async () => {
    if (!isLoggedIn) {
      showToast("Sipariş vermek için önce giriş yapın.");
      return;
    }
    if (cart.length === 0) return;

    try {
      // 1. Backend'deki sepete ürünleri senkronize et
      for (const item of cart) {
        try {
          await api.post("/cart/items", {
            productId: item.product.id,
            quantity: item.quantity,
          });
        } catch {
          // Sepette zaten varsa veya doğrudan ekleme API'si farklıysa devam et
        }
      }

      // 2. Siparişi oluştur
      const payload = {
        couponCode: coupon.trim() ? coupon.trim() : null,
        items: cart.map((i) => ({
          productId: i.product.id,
          quantity: i.quantity,
        })),
      };

      await api.post("/orders", payload);
      setOrderSuccess(true);
      setCart([]);
      setCoupon("");
      setAppliedDiscount(0);
      setCouponStatus(null);
      loadProducts();
      setTimeout(() => setOrderSuccess(false), 5000);
    } catch (err: any) {
      showToast(
        err.response?.data?.error ||
          err.response?.data?.message ||
          "Sipariş işlenirken bir sorun oluştu.",
      );
    }
  };

  return (
    <div
      style={{
        minHeight: "100vh",
        backgroundColor: "#f8fafc",
        color: "#0f172a",
        display: "flex",
        flexDirection: "column",
      }}
    >
      {/* Toast Bildirimi */}
      {notification && (
        <div
          style={{
            position: "fixed",
            bottom: "24px",
            right: "24px",
            backgroundColor: "#0f172a",
            color: "#f8fafc",
            padding: "12px 18px",
            borderRadius: "6px",
            boxShadow: "0 4px 14px rgba(0,0,0,0.12)",
            fontSize: "13px",
            zIndex: 9999,
            display: "flex",
            alignItems: "center",
            gap: "8px",
          }}
        >
          <AlertCircle size={16} />
          {notification}
        </div>
      )}

      {/* Sipariş Geçmişi Modalı */}
      {showHistoryModal && (
        <div
          style={{
            position: "fixed",
            inset: 0,
            backgroundColor: "rgba(15, 23, 42, 0.4)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            zIndex: 1000,
            padding: "16px",
          }}
        >
          <div
            style={{
              backgroundColor: "#fff",
              borderRadius: "8px",
              width: "100%",
              maxWidth: "520px",
              maxHeight: "80vh",
              display: "flex",
              flexDirection: "column",
              boxShadow: "0 10px 25px rgba(0,0,0,0.1)",
            }}
          >
            <div
              style={{
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                padding: "16px 20px",
                borderBottom: "1px solid #e2e8f0",
              }}
            >
              <h3 style={{ fontSize: "15px", fontWeight: "600" }}>
                Geçmiş Siparişlerim
              </h3>
              <button
                onClick={() => setShowHistoryModal(false)}
                style={{
                  background: "none",
                  border: "none",
                  cursor: "pointer",
                  color: "#64748b",
                }}
              >
                <X size={18} />
              </button>
            </div>
            <div style={{ padding: "16px 20px", overflowY: "auto", flex: 1 }}>
              {isLoadingHistory ? (
                <div
                  style={{
                    textAlign: "center",
                    padding: "24px",
                    color: "#64748b",
                    fontSize: "13px",
                  }}
                >
                  Yükleniyor...
                </div>
              ) : orderHistory.length === 0 ? (
                <div
                  style={{
                    textAlign: "center",
                    padding: "24px",
                    color: "#64748b",
                    fontSize: "13px",
                  }}
                >
                  Kayıtlı siparişiniz bulunmamaktadır.
                </div>
              ) : (
                <div
                  style={{
                    display: "flex",
                    flexDirection: "column",
                    gap: "10px",
                  }}
                >
                  {orderHistory.map((order) => (
                    <div
                      key={order.id}
                      style={{
                        border: "1px solid #e2e8f0",
                        borderRadius: "6px",
                        padding: "12px",
                        fontSize: "13px",
                      }}
                    >
                      <div
                        style={{
                          display: "flex",
                          justifyContent: "space-between",
                          marginBottom: "4px",
                          fontWeight: "600",
                        }}
                      >
                        <span>Sipariş No: #{order.id}</span>
                        <span>₺{order.totalAmount?.toFixed(2)}</span>
                      </div>
                      <div style={{ fontSize: "11px", color: "#64748b" }}>
                        Tarih:{" "}
                        {new Date(order.createdAt).toLocaleString("tr-TR")}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Header */}
      <header
        style={{
          backgroundColor: "#ffffff",
          borderBottom: "1px solid #e2e8f0",
          position: "sticky",
          top: 0,
          zIndex: 50,
        }}
      >
        <div
          style={{
            maxWidth: "1200px",
            margin: "0 auto",
            padding: "12px 20px",
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
          }}
        >
          <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
            <div
              style={{
                width: "30px",
                height: "30px",
                backgroundColor: "#0f172a",
                borderRadius: "6px",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                color: "#ffffff",
                fontWeight: "700",
                fontSize: "14px",
              }}
            >
              M
            </div>
            <span
              style={{
                fontSize: "17px",
                fontWeight: "700",
                letterSpacing: "-0.3px",
              }}
            >
              MiniStore
            </span>
          </div>

          <div>
            {isLoggedIn ? (
              <div
                style={{ display: "flex", alignItems: "center", gap: "10px" }}
              >
                <button
                  onClick={fetchOrderHistory}
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: "5px",
                    padding: "6px 12px",
                    fontSize: "12px",
                    backgroundColor: "#f8fafc",
                    border: "1px solid #cbd5e1",
                    borderRadius: "5px",
                    color: "#334155",
                  }}
                >
                  <History size={13} /> Siparişlerim
                </button>
                <span style={{ fontSize: "12px", color: "#64748b" }}>
                  <strong style={{ color: "#0f172a" }}>{email}</strong>
                </span>
                <button
                  onClick={handleLogout}
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: "4px",
                    padding: "6px 10px",
                    fontSize: "12px",
                    backgroundColor: "#ffffff",
                    border: "1px solid #cbd5e1",
                    borderRadius: "5px",
                    color: "#334155",
                  }}
                >
                  <LogOut size={13} /> Çıkış
                </button>
              </div>
            ) : (
              <div
                style={{
                  display: "flex",
                  flexDirection: "column",
                  alignItems: "flex-end",
                  gap: "4px",
                }}
              >
                <form
                  onSubmit={handleLogin}
                  style={{ display: "flex", gap: "6px" }}
                >
                  <input
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    placeholder="E-posta"
                    style={{
                      padding: "6px 10px",
                      fontSize: "12px",
                      border: "1px solid #cbd5e1",
                      borderRadius: "5px",
                      width: "180px",
                    }}
                  />
                  <input
                    type="password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    placeholder="Şifre"
                    style={{
                      padding: "6px 10px",
                      fontSize: "12px",
                      border: "1px solid #cbd5e1",
                      borderRadius: "5px",
                      width: "100px",
                    }}
                  />
                  <button
                    type="submit"
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "4px",
                      padding: "6px 12px",
                      fontSize: "12px",
                      backgroundColor: "#0f172a",
                      color: "#ffffff",
                      border: "none",
                      borderRadius: "5px",
                      fontWeight: "500",
                    }}
                  >
                    <LogIn size={13} /> Giriş
                  </button>
                </form>
                <button
                  onClick={fillDemoCredentials}
                  style={{
                    background: "none",
                    border: "none",
                    color: "#64748b",
                    fontSize: "11px",
                    textDecoration: "underline",
                    cursor: "pointer",
                  }}
                >
                  Demo Hesabı Doldur
                </button>
              </div>
            )}
          </div>
        </div>
      </header>

      {/* Ana Gövde */}
      <main
        style={{
          maxWidth: "1200px",
          margin: "24px auto",
          padding: "0 20px",
          flex: 1,
          width: "100%",
        }}
      >
        {orderSuccess && (
          <div
            style={{
              padding: "12px 16px",
              backgroundColor: "#ecfdf5",
              border: "1px solid #a7f3d0",
              color: "#065f46",
              borderRadius: "6px",
              marginBottom: "20px",
              display: "flex",
              alignItems: "center",
              gap: "8px",
              fontSize: "13px",
            }}
          >
            <PackageCheck size={18} color="#059669" />
            <div>
              <strong>Siparişiniz Tamamlandı!</strong> Veritabanında stoklar
              atomik işlemle düşürüldü.
            </div>
          </div>
        )}

        <div
          style={{
            display: "grid",
            gridTemplateColumns: "1fr 330px",
            gap: "24px",
            alignItems: "start",
          }}
        >
          {/* Sol Kolon: Ürünler */}
          <div>
            {/* Arama ve Filtreler */}
            <div
              style={{
                backgroundColor: "#ffffff",
                border: "1px solid #e2e8f0",
                borderRadius: "6px",
                padding: "12px 14px",
                marginBottom: "16px",
                display: "flex",
                flexDirection: "column",
                gap: "10px",
              }}
            >
              <div style={{ display: "flex", gap: "10px" }}>
                <div style={{ position: "relative", flex: 1 }}>
                  <Search
                    size={15}
                    color="#94a3b8"
                    style={{ position: "absolute", left: "10px", top: "9px" }}
                  />
                  <input
                    type="text"
                    placeholder="Ürün veya model ara..."
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    style={{
                      width: "100%",
                      padding: "7px 10px 7px 32px",
                      fontSize: "13px",
                      border: "1px solid #e2e8f0",
                      borderRadius: "5px",
                      outline: "none",
                    }}
                  />
                </div>

                <div
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: "4px",
                    border: "1px solid #e2e8f0",
                    borderRadius: "5px",
                    padding: "0 8px",
                    backgroundColor: "#f8fafc",
                  }}
                >
                  <ArrowUpDown size={13} color="#64748b" />
                  <select
                    value={sortBy}
                    onChange={(e: any) => setSortBy(e.target.value)}
                    style={{
                      border: "none",
                      background: "transparent",
                      fontSize: "12px",
                      color: "#334155",
                      outline: "none",
                      cursor: "pointer",
                    }}
                  >
                    <option value="default">Varsayılan Sıralama</option>
                    <option value="price-asc">Fiyat (Düşükten Yükseğe)</option>
                    <option value="price-desc">Fiyat (Yüksekten Düşüğe)</option>
                  </select>
                </div>
              </div>

              {/* Kategoriler */}
              <div style={{ display: "flex", gap: "6px", flexWrap: "wrap" }}>
                {categories.map((cat) => (
                  <button
                    key={cat}
                    onClick={() => setSelectedCategory(cat)}
                    style={{
                      padding: "4px 10px",
                      fontSize: "12px",
                      fontWeight: "500",
                      borderRadius: "16px",
                      border:
                        selectedCategory === cat
                          ? "1px solid #0f172a"
                          : "1px solid #e2e8f0",
                      backgroundColor:
                        selectedCategory === cat ? "#0f172a" : "#ffffff",
                      color: selectedCategory === cat ? "#ffffff" : "#64748b",
                    }}
                  >
                    {cat}
                  </button>
                ))}
              </div>
            </div>

            {/* Durum Yönetimi (Hata / Yükleniyor / Liste) */}
            {fetchError ? (
              <div
                style={{
                  textAlign: "center",
                  padding: "36px 16px",
                  backgroundColor: "#fff",
                  border: "1px solid #fee2e2",
                  borderRadius: "6px",
                  color: "#991b1b",
                }}
              >
                <ServerOff
                  size={28}
                  style={{ margin: "0 auto 8px", display: "block" }}
                />
                <div
                  style={{
                    fontWeight: "600",
                    fontSize: "14px",
                    marginBottom: "4px",
                  }}
                >
                  Backend API Servisine Bağlanılamadı
                </div>
                <p
                  style={{
                    fontSize: "12px",
                    color: "#7f1d1d",
                    marginBottom: "12px",
                  }}
                >
                  Backend projesinin <code>http://localhost:5080</code> portunda
                  ayakta olduğundan emin olun.
                </p>
                <button
                  onClick={loadProducts}
                  style={{
                    display: "inline-flex",
                    alignItems: "center",
                    gap: "6px",
                    padding: "6px 14px",
                    fontSize: "12px",
                    backgroundColor: "#991b1b",
                    color: "#fff",
                    border: "none",
                    borderRadius: "4px",
                  }}
                >
                  <RotateCw size={13} /> Yeniden Dene
                </button>
              </div>
            ) : isLoading ? (
              <div
                style={{
                  display: "grid",
                  gridTemplateColumns: "repeat(auto-fill, minmax(230px, 1fr))",
                  gap: "14px",
                }}
              >
                {[1, 2, 3, 4].map((i) => (
                  <div
                    key={i}
                    style={{
                      backgroundColor: "#fff",
                      border: "1px solid #e2e8f0",
                      borderRadius: "6px",
                      padding: "16px",
                      height: "180px",
                      display: "flex",
                      flexDirection: "column",
                      justifyContent: "space-between",
                    }}
                  >
                    <div
                      style={{
                        height: "14px",
                        backgroundColor: "#f1f5f9",
                        borderRadius: "4px",
                        width: "40%",
                      }}
                    />
                    <div
                      style={{
                        height: "18px",
                        backgroundColor: "#f1f5f9",
                        borderRadius: "4px",
                        width: "70%",
                      }}
                    />
                    <div
                      style={{
                        height: "32px",
                        backgroundColor: "#f1f5f9",
                        borderRadius: "4px",
                      }}
                    />
                  </div>
                ))}
              </div>
            ) : processedProducts.length === 0 ? (
              <div
                style={{
                  textAlign: "center",
                  padding: "40px 0",
                  backgroundColor: "#fff",
                  border: "1px solid #e2e8f0",
                  borderRadius: "6px",
                  color: "#64748b",
                  fontSize: "13px",
                }}
              >
                Kriterlere uygun ürün bulunamadı.
              </div>
            ) : (
              <div
                style={{
                  display: "grid",
                  gridTemplateColumns: "repeat(auto-fill, minmax(230px, 1fr))",
                  gap: "14px",
                }}
              >
                {processedProducts.map((p) => {
                  const stock = getStock(p);
                  return (
                    <div
                      key={p.id}
                      style={{
                        backgroundColor: "#ffffff",
                        border: "1px solid #e2e8f0",
                        borderRadius: "6px",
                        padding: "14px",
                        display: "flex",
                        flexDirection: "column",
                        justifyContent: "space-between",
                      }}
                    >
                      <div>
                        <div
                          style={{
                            display: "flex",
                            justifyContent: "space-between",
                            alignItems: "center",
                            marginBottom: "4px",
                          }}
                        >
                          <span
                            style={{
                              fontSize: "11px",
                              fontWeight: "500",
                              color: "#64748b",
                              textTransform: "uppercase",
                            }}
                          >
                            {p.categoryName || "Kategori"}
                          </span>
                          <span
                            style={{
                              fontSize: "11px",
                              padding: "2px 6px",
                              borderRadius: "4px",
                              backgroundColor:
                                stock > 3
                                  ? "#f0fdf4"
                                  : stock > 0
                                    ? "#fefce8"
                                    : "#fef2f2",
                              color:
                                stock > 3
                                  ? "#166534"
                                  : stock > 0
                                    ? "#854d0e"
                                    : "#991b1b",
                              fontWeight: "500",
                            }}
                          >
                            {stock > 3
                              ? `${stock} adet`
                              : stock > 0
                                ? `Son ${stock} adet!`
                                : "Tükendi"}
                          </span>
                        </div>
                        <h3
                          style={{
                            fontSize: "14px",
                            fontWeight: "600",
                            margin: "4px 0",
                            color: "#0f172a",
                          }}
                        >
                          {p.name}
                        </h3>
                        <p
                          style={{
                            fontSize: "12px",
                            color: "#64748b",
                            lineHeight: "1.4",
                            marginBottom: "14px",
                          }}
                        >
                          {p.description}
                        </p>
                      </div>

                      <div>
                        <div
                          style={{
                            fontSize: "16px",
                            fontWeight: "700",
                            color: "#0f172a",
                            marginBottom: "10px",
                          }}
                        >
                          ₺{p.price.toFixed(2)}
                        </div>

                        <button
                          disabled={stock <= 0}
                          onClick={() => addToCart(p)}
                          style={{
                            width: "100%",
                            padding: "7px",
                            backgroundColor: stock > 0 ? "#0f172a" : "#e2e8f0",
                            color: stock > 0 ? "#ffffff" : "#94a3b8",
                            border: "none",
                            borderRadius: "5px",
                            fontSize: "12px",
                            fontWeight: "500",
                            cursor: stock > 0 ? "pointer" : "not-allowed",
                          }}
                        >
                          {stock > 0 ? "Sepete Ekle" : "Stokta Yok"}
                        </button>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>

          {/* Sağ Kolon: Sepet */}
          <div
            style={{
              backgroundColor: "#ffffff",
              border: "1px solid #e2e8f0",
              borderRadius: "6px",
              padding: "16px",
            }}
          >
            <div
              style={{
                display: "flex",
                alignItems: "center",
                justifyContent: "space-between",
                borderBottom: "1px solid #f1f5f9",
                paddingBottom: "10px",
                marginBottom: "12px",
              }}
            >
              <div
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: "6px",
                  fontWeight: "600",
                  fontSize: "14px",
                }}
              >
                <ShoppingCart size={16} />
                Sepetim
              </div>
              <span
                style={{
                  fontSize: "11px",
                  backgroundColor: "#f1f5f9",
                  padding: "2px 6px",
                  borderRadius: "10px",
                  fontWeight: "600",
                }}
              >
                {cart.reduce((a, b) => a + b.quantity, 0)} ürün
              </span>
            </div>

            {/* Kargo Barı */}
            <div
              style={{
                marginBottom: "14px",
                backgroundColor: "#f8fafc",
                border: "1px solid #f1f5f9",
                borderRadius: "5px",
                padding: "8px 10px",
              }}
            >
              <div
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: "6px",
                  fontSize: "11px",
                  color: "#475569",
                  marginBottom: "4px",
                }}
              >
                <Truck size={13} color="#2563eb" />
                {subtotal >= freeShippingThreshold ? (
                  <strong style={{ color: "#16a34a" }}>
                    Ücretsiz kargo hakkı!
                  </strong>
                ) : (
                  <span>
                    ₺{(freeShippingThreshold - subtotal).toFixed(2)} ekle,{" "}
                    <strong>kargo bedava</strong> olsun
                  </span>
                )}
              </div>
              <div
                style={{
                  width: "100%",
                  height: "4px",
                  backgroundColor: "#e2e8f0",
                  borderRadius: "3px",
                  overflow: "hidden",
                }}
              >
                <div
                  style={{
                    width: `${progressToFreeShipping}%`,
                    height: "100%",
                    backgroundColor:
                      progressToFreeShipping === 100 ? "#16a34a" : "#2563eb",
                    transition: "width 0.3s ease",
                  }}
                />
              </div>
            </div>

            {cart.length === 0 ? (
              <div
                style={{
                  textAlign: "center",
                  padding: "20px 0",
                  color: "#94a3b8",
                  fontSize: "12px",
                }}
              >
                Sepetiniz boş.
              </div>
            ) : (
              <div>
                <div
                  style={{
                    display: "flex",
                    flexDirection: "column",
                    gap: "10px",
                    marginBottom: "14px",
                  }}
                >
                  {cart.map((item) => (
                    <div
                      key={item.product.id}
                      style={{
                        display: "flex",
                        justifyContent: "space-between",
                        alignItems: "center",
                        fontSize: "12px",
                        borderBottom: "1px solid #f8fafc",
                        paddingBottom: "6px",
                      }}
                    >
                      <div style={{ flex: 1, paddingRight: "6px" }}>
                        <div style={{ fontWeight: "500", color: "#0f172a" }}>
                          {item.product.name}
                        </div>
                        <div style={{ fontSize: "11px", color: "#64748b" }}>
                          ₺{item.product.price.toFixed(2)}
                        </div>
                      </div>

                      <div
                        style={{
                          display: "flex",
                          alignItems: "center",
                          gap: "4px",
                          marginRight: "8px",
                        }}
                      >
                        <button
                          onClick={() => updateQuantity(item.product.id, -1)}
                          style={{
                            width: "22px",
                            height: "22px",
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "center",
                            backgroundColor: "#f1f5f9",
                            border: "1px solid #e2e8f0",
                            borderRadius: "3px",
                          }}
                        >
                          <Minus size={11} />
                        </button>
                        <span
                          style={{
                            minWidth: "14px",
                            textAlign: "center",
                            fontWeight: "600",
                            fontSize: "11px",
                          }}
                        >
                          {item.quantity}
                        </span>
                        <button
                          onClick={() => updateQuantity(item.product.id, 1)}
                          style={{
                            width: "22px",
                            height: "22px",
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "center",
                            backgroundColor: "#f1f5f9",
                            border: "1px solid #e2e8f0",
                            borderRadius: "3px",
                          }}
                        >
                          <Plus size={11} />
                        </button>
                      </div>

                      <button
                        onClick={() => removeFromCart(item.product.id)}
                        style={{
                          background: "none",
                          border: "none",
                          color: "#94a3b8",
                          cursor: "pointer",
                          padding: "2px",
                        }}
                      >
                        <Trash2 size={14} />
                      </button>
                    </div>
                  ))}
                </div>

                {/* Kupon */}
                <div
                  style={{
                    borderTop: "1px solid #f1f5f9",
                    paddingTop: "10px",
                    marginBottom: "10px",
                  }}
                >
                  <div
                    style={{ display: "flex", gap: "5px", marginBottom: "4px" }}
                  >
                    <input
                      type="text"
                      placeholder="Kupon (INDIRIM10)"
                      value={coupon}
                      onChange={(e) => setCoupon(e.target.value)}
                      style={{
                        flex: 1,
                        padding: "5px 8px",
                        fontSize: "11px",
                        border: "1px solid #cbd5e1",
                        borderRadius: "4px",
                      }}
                    />
                    <button
                      onClick={applyCoupon}
                      style={{
                        padding: "5px 10px",
                        backgroundColor: "#f8fafc",
                        border: "1px solid #cbd5e1",
                        borderRadius: "4px",
                        fontSize: "11px",
                        fontWeight: "500",
                      }}
                    >
                      Uygula
                    </button>
                  </div>
                  {couponStatus && (
                    <div
                      style={{
                        fontSize: "11px",
                        color: couponStatus.isError ? "#dc2626" : "#16a34a",
                        fontWeight: "500",
                      }}
                    >
                      {couponStatus.msg}
                    </div>
                  )}
                </div>

                {/* Tutar */}
                <div
                  style={{
                    borderTop: "1px solid #f1f5f9",
                    paddingTop: "8px",
                    marginBottom: "14px",
                  }}
                >
                  <div
                    style={{
                      display: "flex",
                      justifyContent: "space-between",
                      fontSize: "12px",
                      color: "#64748b",
                      marginBottom: "3px",
                    }}
                  >
                    <span>Ara Toplam:</span>
                    <span>₺{subtotal.toFixed(2)}</span>
                  </div>
                  {appliedDiscount > 0 && (
                    <div
                      style={{
                        display: "flex",
                        justifyContent: "space-between",
                        fontSize: "12px",
                        color: "#16a34a",
                        marginBottom: "3px",
                      }}
                    >
                      <span>Kupon İndirimi (%10):</span>
                      <span>-₺{(subtotal * appliedDiscount).toFixed(2)}</span>
                    </div>
                  )}
                  <div
                    style={{
                      display: "flex",
                      justifyContent: "space-between",
                      fontSize: "15px",
                      fontWeight: "700",
                      color: "#0f172a",
                      marginTop: "6px",
                      borderTop: "1px dashed #e2e8f0",
                      paddingTop: "6px",
                    }}
                  >
                    <span>Ödenecek Tutar:</span>
                    <span>₺{total.toFixed(2)}</span>
                  </div>
                </div>

                <button
                  onClick={handleCheckout}
                  style={{
                    width: "100%",
                    padding: "9px",
                    backgroundColor: "#16a34a",
                    color: "#ffffff",
                    border: "none",
                    borderRadius: "5px",
                    fontWeight: "600",
                    fontSize: "13px",
                  }}
                >
                  Siparişi Tamamla
                </button>
              </div>
            )}
          </div>
        </div>
      </main>

      {/* Footer */}
      <footer
        style={{
          borderTop: "1px solid #e2e8f0",
          backgroundColor: "#ffffff",
          padding: "16px 20px",
          marginTop: "32px",
        }}
      >
        <div
          style={{
            maxWidth: "1200px",
            margin: "0 auto",
            textAlign: "center",
            fontSize: "12px",
            color: "#64748b",
          }}
        >
          © 2026 MiniStore. Tüm hakları saklıdır.
        </div>
      </footer>
    </div>
  );
}
