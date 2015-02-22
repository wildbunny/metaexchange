-- phpMyAdmin SQL Dump
-- version 3.2.4
-- http://www.phpmyadmin.net
--
-- Host: localhost
-- Generation Time: Feb 22, 2015 at 02:31 PM
-- Server version: 5.1.41
-- PHP Version: 5.3.1

SET SQL_MODE="NO_AUTO_VALUE_ON_ZERO";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;

--
-- Database: `metaexchangesite`
--

-- --------------------------------------------------------

--
-- Table structure for table `markets`
--

CREATE TABLE IF NOT EXISTS `markets` (
  `symbol_pair` varchar(20) NOT NULL,
  `ask` decimal(18,8) NOT NULL,
  `bid` decimal(18,8) NOT NULL,
  `ask_max` decimal(18,8) NOT NULL COMMENT 'quantity of base currency',
  `bid_max` decimal(18,8) NOT NULL COMMENT 'quantity of quote currency',
  `ask_fee_percent` decimal(18,8) NOT NULL DEFAULT '0.00000000',
  `bid_fee_percent` decimal(18,8) NOT NULL DEFAULT '0.00000000',
  `daemon_url` varchar(255) NOT NULL,
  `last_tid` int(10) unsigned NOT NULL,
  `up` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`symbol_pair`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `sender_to_deposit`
--

CREATE TABLE IF NOT EXISTS `sender_to_deposit` (
  `receiving_address` varchar(64) NOT NULL,
  `deposit_address` varchar(36) NOT NULL,
  `symbol_pair` varchar(20) NOT NULL,
  PRIMARY KEY (`receiving_address`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- --------------------------------------------------------

--
-- Table structure for table `transactions`
--

CREATE TABLE IF NOT EXISTS `transactions` (
  `uid` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `received_txid` varchar(64) NOT NULL,
  `sent_txid` varchar(64) DEFAULT NULL,
  `symbol_pair` varchar(20) NOT NULL,
  `order_type` enum('none','buy','sell') NOT NULL,
  `amount` decimal(18,8) NOT NULL DEFAULT '0.00000000',
  `price` decimal(18,8) NOT NULL DEFAULT '0.00000000',
  `fee` decimal(18,8) NOT NULL DEFAULT '0.00000000',
  `date` datetime NOT NULL,
  `notes` varchar(64) DEFAULT NULL,
  `status` enum('none','processing','completed','refunded') NOT NULL,
  `deposit_address` varchar(36) DEFAULT NULL,
  PRIMARY KEY (`uid`),
  UNIQUE KEY `received_txid` (`received_txid`)
) ENGINE=InnoDB  DEFAULT CHARSET=latin1 AUTO_INCREMENT=232 ;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
