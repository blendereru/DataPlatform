CREATE TABLE customers (
                           customer_id SERIAL PRIMARY KEY,
                           email VARCHAR(100) UNIQUE,
                           first_name VARCHAR(50),
                           last_name VARCHAR(50),
                           created_at TIMESTAMP DEFAULT NOW(),
                           customer_segment VARCHAR(20) DEFAULT 'regular'
);

CREATE TABLE products (
                          product_id SERIAL PRIMARY KEY,
                          product_name VARCHAR(100),
                          category VARCHAR(50),
                          price NUMERIC(10,2),
                          stock_quantity INT
);

CREATE TABLE orders (
                        order_id SERIAL PRIMARY KEY,
                        customer_id INT REFERENCES customers(customer_id),
                        order_date TIMESTAMP DEFAULT NOW(),
                        status VARCHAR(20) DEFAULT 'pending',
                        total_amount NUMERIC(10,2),
                        updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE order_items (
                             order_item_id SERIAL PRIMARY KEY,
                             order_id INT REFERENCES orders(order_id),
                             product_id INT REFERENCES products(product_id),
                             quantity INT,
                             unit_price NUMERIC(10,2),
                             line_total NUMERIC(10,2)
);

-- Insert sample data
INSERT INTO customers (email, first_name, last_name, customer_segment) VALUES
                                                                           ('john@example.com', 'John', 'Doe', 'premium'),
                                                                           ('jane@example.com', 'Jane', 'Smith', 'regular'),
                                                                           ('bob@example.com', 'Bob', 'Johnson', 'premium'),
                                                                           ('alice@example.com', 'Alice', 'Williams', 'regular'),
                                                                           ('charlie@example.com', 'Charlie', 'Brown', 'vip');

INSERT INTO products (product_name, category, price, stock_quantity) VALUES
                                                                         ('Laptop Pro', 'Electronics', 1299.99, 50),
                                                                         ('Wireless Mouse', 'Electronics', 29.99, 200),
                                                                         ('Office Chair', 'Furniture', 299.99, 30),
                                                                         ('Desk Lamp', 'Furniture', 49.99, 100),
                                                                         ('Notebook Set', 'Stationery', 15.99, 500);

INSERT INTO orders (customer_id, order_date, status, total_amount) VALUES
                                                                       (1, NOW() - INTERVAL '5 days', 'completed', 1329.98),
                                                                       (2, NOW() - INTERVAL '4 days', 'completed', 299.99),
                                                                       (3, NOW() - INTERVAL '3 days', 'completed', 65.98),
                                                                       (1, NOW() - INTERVAL '2 days', 'processing', 49.99),
                                                                       (4, NOW() - INTERVAL '1 day', 'completed', 1299.99),
                                                                       (5, NOW(), 'pending', 345.97);

INSERT INTO order_items (order_id, product_id, quantity, unit_price, line_total) VALUES
                                                                                     (1, 1, 1, 1299.99, 1299.99),
                                                                                     (1, 2, 1, 29.99, 29.99),
                                                                                     (2, 3, 1, 299.99, 299.99),
                                                                                     (3, 2, 1, 29.99, 29.99),
                                                                                     (3, 5, 2, 15.99, 31.98),
                                                                                     (4, 4, 1, 49.99, 49.99),
                                                                                     (5, 1, 1, 1299.99, 1299.99),
                                                                                     (6, 3, 1, 299.99, 299.99),
                                                                                     (6, 4, 1, 49.99, 49.99);


CREATE INDEX idx_orders_customer ON orders(customer_id);
CREATE INDEX idx_orders_date ON orders(order_date);
CREATE INDEX idx_order_items_order ON order_items(order_id);